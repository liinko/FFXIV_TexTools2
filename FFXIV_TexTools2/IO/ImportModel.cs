// FFXIV TexTools
// Copyright © 2017 Rafael Gonzalez - All Rights Reserved
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using FFXIV_TexTools2.FileTypes.ModelContainers;
using FFXIV_TexTools2.Helpers;
using FFXIV_TexTools2.Material.ModelMaterial;
using FFXIV_TexTools2.Model;
using FFXIV_TexTools2.Resources;
using HelixToolkit.Wpf.SharpDX;
using HelixToolkit.Wpf.SharpDX.Core;
using Newtonsoft.Json;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml;

namespace FFXIV_TexTools2.IO
{
	public class ImportModel
	{

		/// <summary>
		/// The amount to add or subtract from the offste
		/// </summary>
		/// <remarks>
		/// This is calculated by multiplying the dat file number by 16 bytes [.dat4]: (4 * 16 = 64)
		/// This amount is added to the offset when reading and subtracted from the offset when writing
		/// </remarks>

		static Dictionary<int, int> nVertDict = new Dictionary<int, int>();
		static Dictionary<string, ImportSettings> importSettings = new Dictionary<string, ImportSettings>();



		public static int ImportOBJ(string category, string itemName, string modelName, string selectedMesh, string internalPath)
		{
			var savePath = Properties.Settings.Default.Save_Directory + "/" + category + "/" + itemName + "/3D/" + modelName + "_mesh_" + selectedMesh + ".obj";

			if (File.Exists(savePath))
			{
				var lines = File.ReadAllLines(savePath);

				Vector3Collection Vertex = new Vector3Collection();
				Vector2Collection TexCoord = new Vector2Collection();
				Vector2Collection TexCoord2 = new Vector2Collection();
				Vector3Collection Normals = new Vector3Collection();
				IntCollection Indices = new IntCollection();

				char[] delimiterChars = { ' ' };

				foreach (var l in lines)
				{
					var s = l.Split(delimiterChars);
					if (s[0].Equals("v"))
					{
						Vertex.Add(new SharpDX.Vector3(float.Parse(s[1]), float.Parse(s[2]), float.Parse(s[3])));
					}
					else if (s[0].Equals("vt"))
					{
						TexCoord.Add(new SharpDX.Vector2(float.Parse(s[1]), float.Parse(s[2])));
					}
					else if (s[0].Equals("vn"))
					{
						Normals.Add(new SharpDX.Vector3(float.Parse(s[1]), float.Parse(s[2]), float.Parse(s[3])));
					}
					else if (s[0].Equals("f"))
					{
						var i1 = s[1].Substring(0, s[1].IndexOf("/"));
						Indices.Add(int.Parse(i1) - 1);

						var i2 = s[2].Substring(0, s[2].IndexOf("/"));
						Indices.Add(int.Parse(i2) - 1);

						var i3 = s[3].Substring(0, s[3].IndexOf("/"));
						Indices.Add(int.Parse(i3) - 1);
					}
				}
			}
			return 0;
		}

		public static void ImportDAE(string category, string itemName, string modelName, string selectedMesh, string internalPath, ModelData modelData, Dictionary<string, ImportSettings> settings)
		{
            // Tracks to see if we have any UV2 data all.
            // Only used for warning handling.
            bool anyTexCoord2Data = false;

            importSettings = settings;
            var numMeshes = modelData.LoD[0].MeshCount;

            var savePath = Properties.Settings.Default.Save_Directory + "/" + category + "/" + itemName + "/3D/" + modelName + ".DAE";

			if (importSettings != null)
			{
				savePath = importSettings[Strings.All].path;
			}

			var settingsFile = Path.Combine(Path.GetDirectoryName(savePath), Path.GetFileNameWithoutExtension(savePath) + "_Settings.xml");

			if (importSettings == null && File.Exists(settingsFile))
			{
				importSettings = new Dictionary<string, ImportSettings>();
				using (XmlReader reader = XmlReader.Create(settingsFile))
				{
					while (reader.Read())
					{
						if (reader.IsStartElement())
						{
							if (reader.Name.Equals("Mesh"))
							{
								var name = reader["name"];
								importSettings.Add(name, new ImportSettings());
								while (reader.Read())
								{
									if (reader.IsStartElement())
									{
										if (reader.Name.Contains("Fix"))
										{
											importSettings[name].Fix = bool.Parse(reader.ReadElementContentAsString().ToLower());
										}

										if (reader.Name.Contains("DisableHide"))
										{
											importSettings[name].Disable = bool.Parse(reader.ReadElementContentAsString().ToLower());
											break;
										}
									}
								}
							}
						}
					}
				}
			} else if(importSettings == null)
            {
                // Create default settings if we don't have any.
                importSettings = new Dictionary<string, ImportSettings>();
                importSettings.Add(Strings.All, new ImportSettings());
                for (int i = 0; i < numMeshes; i++)
                {
                    importSettings.Add(i.ToString(), new ImportSettings());
                    importSettings[i.ToString()].path = savePath;
                }
            }

			if (File.Exists(savePath))
			{
				Dictionary<int, ColladaData> cdDict = new Dictionary<int, ColladaData>();

				Dictionary<int, Dictionary<int, ColladaData>> pDict = new Dictionary<int, Dictionary<int, ColladaData>>();

				for (int i = 0; i < numMeshes; i++)
				{
					cdDict.Add(i, new ColladaData());
					pDict.Add(i, new Dictionary<int, ColladaData>());
				}

				Dictionary<string, string> boneJointDict = new Dictionary<string, string>();

				string texc = "-map0-array";
                string texc2 = "-map1-array";

                string vcol = "-col0-array";
                string valpha = "-map2-array";

                string texcBase = "map0";
                string texc2Base = "map1";
                string vcolBase = "col0";
                string valphaBase = "map2";


                string pos = "-positions-array";
				string norm = "-normals-array";
				string biNorm = "-texbinormals";
				string tang = "-textangents";
				int tcStride = 2;
				bool blender = false;

                Dictionary<string, int> CustomBoneSet = new Dictionary<string, int>();
                List<Dictionary<string, int>> OriginalBoneSets = new List<Dictionary<string, int>>();

                using (XmlReader reader = XmlReader.Create(savePath))
				{
					while (reader.Read())
					{
						if (reader.IsStartElement())
						{
							if (reader.Name.Equals("visual_scene"))
							{
								while (reader.Read())
								{
									if (reader.IsStartElement())
									{
										if (reader.Name.Contains("node"))
										{
											var sid = reader["sid"];
											if (sid != null)
											{
												var name = reader["name"];

											    try
											    {
											        boneJointDict.Add(sid, name);
											    }
											    catch (Exception e)
											    {
											        FlexibleMessageBox.Show("Duplicate bone found.\n" +
											                                "Bone: " + sid + "\n\n" +
											                                "Delete the duplicate bone and try again.\n\n" +
											                                "Error: " + e.Message, "ImportModel Error " + Info.appVersion, MessageBoxButtons.OK, MessageBoxIcon.Error);
                                                    return;
											    }

                                            }
										}
									}
								}
								break;
							}
						}
					}
				}

				if(boneJointDict.Count < 1)
				{
					FlexibleMessageBox.Show("No bones were found in the .dae file.", "ImportModel Error " + Info.appVersion, MessageBoxButtons.OK, MessageBoxIcon.Error);
					return;
				}
                
				Dictionary<string, string> meshNameDict = new Dictionary<string, string>();
				List<string> extraBones = new List<string>();

                if (importSettings[Strings.All].UseOriginalBones)
                {
                    for(int i = 0; i < modelData.BoneSet.Count; i++)
                    {
                        OriginalBoneSets.Add(new Dictionary<string, int>(modelData.BoneSet[i].BoneCount));
                        for (int x = 0; x < modelData.BoneSet[i].BoneCount; x++) {
                            int boneId = modelData.BoneSet[i].BoneData[x];
                            OriginalBoneSets[i].Add(modelData.Bones[boneId], x);
                        }

                    }
                }
                else
                {
                    for (int i = 0; i < modelData.Bones.Count; i++)
                    {
                        CustomBoneSet.Add(modelData.Bones[i], i);
                    }
                }

				try
				{
					using (XmlReader reader = XmlReader.Create(savePath))
					{
						while (reader.Read())
						{
							if (reader.IsStartElement())
							{
								if (reader.Name.Contains("authoring_tool"))
								{
									var tool = reader.ReadElementContentAsString();

									if (tool.Contains("OpenCOLLADA"))
                                    {
                                        vcol = "-map0-array";
                                        texc = "-map1-array";
										texc2 = "-map2-array";
                                        valpha = "-map3-array";

                                        biNorm = "-map1-texbinormals";
										tang = "-map1-textangents";

                                        vcolBase = "map0";
                                        texcBase = "map1";
                                        texc2Base = "map2";
                                        valphaBase = "map3";

                                        tcStride = 3;
									}
									else if (tool.Contains("FBX"))
									{
                                        //TODO: ? Set up vertex color importing for blender/FBX?
                                        // Do we even actually support blender/FBX at this point?
										pos = "-position-array";
										norm = "-normal0-array";
										texc = "-uv0-array";
										texc2 = "-uv1-array";

                                        texcBase = "uv0";
                                        texc2Base = "uv1";
                                    }
									else if (tool.Contains("Exporter for Blender"))
									{
										biNorm = "-bitangents-array";
										tang = "-tangents-array";
										texc = "-texcoord-0-array";
										texc2 = "-texcoord-1-array";

                                        texcBase = "texcoord-0";
                                        texc2Base = "texcoord-1";
                                        blender = true;
									}
									else if (!tool.Contains("TexTools"))
									{
										FlexibleMessageBox.Show("Unsupported Authoring Tool\n\n" +
											tool +
											"\n\nCurrently supported tools are:\n" +
											"* 3DS Max with default(FBX) or OpenCOLLADA plugin\n" +
											"* Blender with \"Better\" Collada exporter plugin\n\n" +
											"If you'd like to get another tool supported, submit a request.", "Unsupported File " + Info.appVersion, MessageBoxButtons.OK, MessageBoxIcon.Error);
										return;
									}
								}

								//go to geometry element
								if (reader.Name.Equals("geometry"))
								{
									var atr = reader["name"];
									var id = reader["id"];

								    try
								    {
								        meshNameDict.Add(id, atr);
								    }
								    catch (Exception e)
								    {
								        FlexibleMessageBox.Show("Duplicate mesh found.\n" +
								                                "Mesh: " + id + "\n\n" +
								                                "Full Name: " + atr + "\n\n" +
                                                                "Delete or Rename the duplicate mesh and try again.\n\n" +
								                                "Error: " + e.Message, "ImportModel Error " + Info.appVersion, MessageBoxButtons.OK, MessageBoxIcon.Error);
								        return;
								    }



                                    var vertexOffset = -1;
                                    var normalOffset = -1;
                                    var texCoord1Offset = -1;
                                    var texCoord2Offset = -1;
                                    var vColorOffset = -1;
                                    var vAlphaOffset = -1;
                                    var binormalOffset = -1;
                                    var totalStride = -1;



                                    var meshNum = int.Parse(atr.Substring(atr.LastIndexOf("_") + 1, 1));

                                    if (atr.Contains("."))
									{
                                        try
                                        {
                                            meshNum = int.Parse(atr.Substring(atr.LastIndexOf("_") + 1, atr.LastIndexOf(".") - (atr.LastIndexOf("_") + 1)));
                                        } catch (Exception e)
                                        {

                                        }
                                    }

									var cData = new ColladaData();

									while (reader.Read())
									{
                                        if (reader.IsStartElement())
                                        {
                                            if (reader.Name.Contains("float_array"))
                                            {
                                                //Vertex 
                                                if (reader["id"].ToLower().Contains(pos))
                                                {
                                                    cData.vertex.AddRange((float[])reader.ReadElementContentAs(typeof(float[]), null));
                                                }
                                                //Normals
                                                else if (reader["id"].ToLower().Contains(norm) && cData.vertex.Count > 0)
                                                {
                                                    cData.normal.AddRange((float[])reader.ReadElementContentAs(typeof(float[]), null));
                                                }
                                                //Texture Coordinates
                                                else if (reader["id"].ToLower().Contains(texc) && cData.vertex.Count > 0)
                                                {
                                                    cData.texCoord.AddRange((float[])reader.ReadElementContentAs(typeof(float[]), null));
                                                }
                                                //Texture Coordinates2
                                                else if (reader["id"].ToLower().Contains(texc2) && cData.vertex.Count > 0)
                                                {
                                                    cData.texCoord2.AddRange((float[])reader.ReadElementContentAs(typeof(float[]), null));
                                                    anyTexCoord2Data = true;
                                                }
                                                //Tangents
                                                else if (reader["id"].ToLower().Contains(tang) && cData.vertex.Count > 0)
                                                {
                                                    cData.tangent.AddRange((float[])reader.ReadElementContentAs(typeof(float[]), null));
                                                }
                                                //BiNormals
                                                else if (reader["id"].ToLower().Contains(biNorm) && cData.vertex.Count > 0)
                                                {
                                                    cData.biNormal.AddRange((float[])reader.ReadElementContentAs(typeof(float[]), null));
                                                }
                                                //Vertex Color
                                                else if (reader["id"].ToLower().Contains(vcol) && cData.vertex.Count > 0)
                                                {
                                                    cData.vertexColors.AddRange((float[])reader.ReadElementContentAs(typeof(float[]), null));
                                                }
                                                //Vertex Alpha
                                                else if (reader["id"].ToLower().Contains(valpha) && cData.vertex.Count > 0)
                                                {
                                                    cData.vertexAlphas.AddRange((float[])reader.ReadElementContentAs(typeof(float[]), null));
                                                }
                                            }

                                            // Triangle Header precedes Index block,
                                            // And contains all our stride information.
                                            if (reader.Name.Equals("triangles")) {

                                                // At this point we've read all of our original basic data, 
                                                // so time to massage the data if the data sucks.

                                                if(cData.biNormal.Count == 0)
                                                {
                                                    // If we have no binormal data...
                                                    // I guess just error?  Not sure what an appropriate dummy value would be.

                                                    FlexibleMessageBox.Show("Mesh " + meshNum + " had no BiNormal Data.\nPlease check your DAE Exporter Settings.", "ImportModel Error " + Info.appVersion, MessageBoxButtons.OK, MessageBoxIcon.Error);
                                                    return;
                                                }

                                                if(cData.texCoord2.Count == 0)
                                                {
                                                    // If we have no TexCoord2 data, just clone the TexCoord 1 data.
                                                    cData.texCoord2.AddRange(cData.texCoord);
                                                }

                                                if (cData.vertexColors.Count == 0)
                                                {
                                                    // If we have no VertexColor data, just add a 1.0 value for everything.
                                                    cData.vertexColors.Add(1.0f);
                                                    cData.vertexColors.Add(1.0f);
                                                    cData.vertexColors.Add(1.0f);
                                                }

                                                if (cData.vertexAlphas.Count == 0)
                                                {
                                                    // If we have no VertexColor data, just add a single 1.0 value for everything.
                                                    cData.vertexAlphas.Add(1.0f);
                                                    cData.vertexAlphas.Add(0.0f);
                                                    if (tcStride == 3)
                                                    {
                                                        cData.vertexAlphas.Add(0.0f);
                                                    }
                                                }

                                                while (reader.Read())
                                                {
                                                    if(reader.Name.Equals("input"))
                                                    {
                                                        var name = reader.GetAttribute("semantic").ToLower();
                                                        var source = reader.GetAttribute("source").ToLower();
                                                        var val = Int32.Parse(reader.GetAttribute("offset"));

                                                        // Make sure we determine the total number of elements
                                                        // So that we can set our stride properly.
                                                        if(val >= totalStride)
                                                        {
                                                            totalStride = val + 1;
                                                        }

                                                        // Now, match semantic names.

                                                        switch(name)
                                                        {
                                                            case "vertex":
                                                                vertexOffset = val;
                                                                break;
                                                            case "normal":
                                                                normalOffset = val;
                                                                break;
                                                            case "color":
                                                                vColorOffset = val;
                                                                break;
                                                            case "texcoord":
                                                                if(source.Contains(texcBase)) {
                                                                    texCoord1Offset = val;
                                                                } else if(source.Contains(texc2Base)) {
                                                                    texCoord2Offset = val;
                                                                } else if(source.Contains(valphaBase))
                                                                {
                                                                    // Vertex Alpha is stored in UV3 S coordinate, due to inconsistency
                                                                    // issues with .DAE vertex alpha support.
                                                                    vAlphaOffset = val;
                                                                }
                                                                break;
                                                            case "texbinormal":
                                                                if (source.Contains(texcBase))
                                                                {
                                                                    binormalOffset = val;
                                                                }
                                                                break;
                                                            default:
                                                                break;
                                                        }

                                                    } else if(reader.Name.Equals("p"))
                                                    {
                                                        cData.indexStride = totalStride;
                                                        break;
                                                    }
                                                }
                                            }
                                            
                                            //Indices
                                            if (reader.Name.Equals("p"))
											{
												cData.index.AddRange((int[])reader.ReadElementContentAs(typeof(int[]), null));
												

												for (int i = 0; i < cData.index.Count; i += cData.indexStride)
												{
													cData.vIndexList.Add(cData.index[i + vertexOffset]);
													cData.nIndexList.Add(cData.index[i + normalOffset]);
													cData.tcIndexList.Add(cData.index[i + texCoord1Offset]);

													if (texCoord2Offset != -1)
													{
														cData.tc2IndexList.Add(cData.index[i + texCoord2Offset]);
													}

													if (binormalOffset != -1)
													{
														cData.bnIndexList.Add(cData.index[i + binormalOffset]);
													}

                                                    if(vColorOffset != -1)
                                                    {
                                                        cData.vcIndexList.Add(cData.index[i + vColorOffset]);
                                                    }

                                                    if (vAlphaOffset != -1)
                                                    {
                                                        cData.vaIndexList.Add(cData.index[i + vAlphaOffset]);
                                                    }

                                                }

                                                if (cData.tc2IndexList.Count == 0)
                                                {
                                                    // If we have no Tex2 Indices, clone the Tex1 Indexes (Code above copied in the Tex1 data).
                                                    cData.tc2IndexList.AddRange(cData.tcIndexList);
                                                }

                                                if (cData.vcIndexList.Count == 0)
                                                {
                                                    // If we have no Vertex Color Indices, initialize and set them to 0.
                                                    var arr = new List<int>(cData.vIndexList.Count);
                                                    foreach(var idx in cData.vIndexList)
                                                    {
                                                        arr.Add(0);
                                                    }
                                                    cData.vcIndexList.AddRange(arr);
                                                }

                                                if (cData.vaIndexList.Count == 0)
                                                {
                                                    // If we have no Vertex Alpha Indices, initialize and set them to 0.
                                                    var arr = new List<int>(cData.vIndexList.Count);
                                                    foreach (var idx in cData.vIndexList)
                                                    {
                                                        arr.Add(0);
                                                    }
                                                    cData.vaIndexList.AddRange(arr);
                                                }

                                                break;
											}
										}
									}

									if (atr.Contains("."))
									{
										var num = atr.Substring(atr.LastIndexOf(".") + 1);
									    try
									    {
                                            if (pDict[meshNum].ContainsKey(int.Parse(num)))
                                            {
                                                FlexibleMessageBox.Show("Duplicate mesh part found.\n" +
                                                                        "Mesh: " + meshNum + "\tPart: " + num + "\n" +
                                                                        "Full Name: " + atr + "\n\n" +
                                                                        "Delete or Rename the duplicate mesh part and try again.\n\n"
                                                                        , "ImportModel Error " + Info.appVersion, MessageBoxButtons.OK, MessageBoxIcon.Error);
                                                return;
                                            }
                                            else
                                            {
                                                int partNum = int.Parse(num);
                                                pDict[meshNum].Add(partNum, cData);

                                                while( partNum + 1 > modelData.LoD[0].MeshList[meshNum].MeshPartList.Count )
                                                {
                                                    var newPart = new MeshPart();
                                                    modelData.LoD[0].MeshList[meshNum].MeshPartList.Add(newPart);
                                                }
                                            }
									    }
									    catch (Exception e)
                                        {
                                            FlexibleMessageBox.Show("Unable to parse Mesh Name.  Please make sure the Mesh Name ends with _X.Y\nWhere X and Y are the Mesh Group and Mesh Part number.\nFull Mesh Name:\n\n" + atr
                                                                    , "ImportModel Error " + Info.appVersion, MessageBoxButtons.OK, MessageBoxIcon.Error);
                                            return;
                                        }
									}
									else
									{
									    try
                                        {
                                            // We have a new mesh; create a new mesh entry for it.
                                            if (meshNum > 0 && !pDict.ContainsKey(meshNum))
                                            {
                                                for (int l = 0; l < 3; l++) {
                                                    if (l == 0)
                                                    {
                                                        cdDict.Add(meshNum, new ColladaData());
                                                        pDict.Add(meshNum, new Dictionary<int, ColladaData>());
                                                    }

                                                    var mesh = new Mesh();
                                                    var newPart = new MeshPart();
                                                    mesh.MeshPartList = new List<MeshPart>();

                                                    mesh.MeshPartList.Add(newPart);
                                                    modelData.LoD[l].MeshList.Add(mesh);
                                                    modelData.LoD[l].MeshCount += 1;
                                                }

                                                numMeshes++;
                                            }

                                            pDict[meshNum].Add(0, cData);

									    }
									    catch (Exception e)
									    {
									        FlexibleMessageBox.Show("Duplicate mesh found.\n" +
									                                "Mesh: " + meshNum + "\tPart: 0" + "\n" +
									                                "Full Name: " + atr + "\n\n" +
                                                                    "Delete or Rename the duplicate mesh part and try again.\n\n" +
									                                "Error: " + e.Message, "ImportModel Error " + Info.appVersion, MessageBoxButtons.OK, MessageBoxIcon.Error);
                                            return;
                                        }

                                    }
								}
								//go to controller element
								else if (reader.Name.Equals("controller"))
								{
									var atr = reader["id"];
									var meshNumPart = 0;
									ColladaData cData;

									if (blender)
									{
										while (reader.Read())
										{
											if (reader.IsStartElement())
											{
												if (reader.Name.Equals("skin"))
												{
													var skinSource = reader["source"];
													atr = meshNameDict[skinSource.Substring(1, skinSource.Length - 1)];
													break;
												}
											}
										}
									}

									var meshNum = int.Parse(atr.Substring(atr.LastIndexOf("_") + 1, 1));

									if (atr.Contains("."))
									{
										meshNum = int.Parse(atr.Substring(atr.LastIndexOf("_") + 1, atr.LastIndexOf(".") - (atr.LastIndexOf("_") + 1)));
									}

									var mDict = pDict[meshNum];

									if (atr.Contains("."))
									{
										meshNumPart = int.Parse(atr.Substring((atr.LastIndexOf(".") + 1), atr.LastIndexOf("-") - (atr.LastIndexOf(".") + 1)));
										cData = mDict[meshNumPart];
									}
									else
									{
										cData = mDict[0];
									}

									while (reader.Read())
									{
										if (reader.IsStartElement())
										{

											if (reader.Name.Contains("Name_array"))
											{
												cData.bones = (string[])reader.ReadElementContentAs(typeof(string[]), null);
											}

                                            if (reader.Name.Contains("float_array"))
                                            {
                                                //Blend Weight
                                                if (reader["id"].ToLower().Contains("weights-array"))
                                                {
                                                    cData.weights.AddRange((float[])reader.ReadElementContentAs(typeof(float[]), null));
                                                }
                                            }
                                            //Blend counts
                                            else if (reader.Name.Equals("vcount"))
                                            {
                                                cData.vCount.AddRange((int[])reader.ReadElementContentAs(typeof(int[]), null));
                                            }
                                            //Blend Indices
                                            else if (reader.Name.Equals("v"))
                                            {
                                                var tempbIndex = (int[])reader.ReadElementContentAs(typeof(int[]), null);

                                                for (int a = 0; a < tempbIndex.Length; a += 2)
                                                {
                                                    var blend = tempbIndex[a];
                                                    var blendName = cData.bones[blend];
                                                    string blendBoneName;

                                                    try
                                                    {
                                                        blendBoneName = boneJointDict[blendName];
                                                    }
                                                    catch (Exception e)
                                                    {
                                                        FlexibleMessageBox.Show("Error reading bone data.\n\nBone: " + blendName +
                                                                                "\n\n" + e.Message, "ImportModel Error " + Info.appVersion, MessageBoxButtons.OK, MessageBoxIcon.Error);
                                                        return;
                                                    }


                                                    var bString = blendBoneName;
                                                    if (!blendBoneName.Contains("h0"))
                                                    {
                                                        bString = Regex.Replace(blendBoneName, @"[\d]", string.Empty);
                                                    }


                                                    if (importSettings[Strings.All].UseOriginalBones)
                                                    {
                                                        var boneSet = OriginalBoneSets[modelData.LoD[0].MeshList[meshNum].BoneListIndex];

                                                        try
                                                        {
                                                            cData.bIndex.Add(boneSet[bString]);
                                                        }
                                                        catch (Exception e)
                                                        {
                                                            FlexibleMessageBox.Show("Bone Addition not allowed when using original bones.\n The import has been cancelled.", "ImportModel Error " + Info.appVersion, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                                            return;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        if (!CustomBoneSet.ContainsKey(bString))
                                                        {
                                                            // Add the bone into our bone dictionary.
                                                            CustomBoneSet.Add(bString, CustomBoneSet.Count);
                                                            if (!extraBones.Contains(bString))
                                                            {
                                                                extraBones.Add(bString);
                                                            }
                                                        }

                                                        try
                                                        {
                                                            cData.bIndex.Add(CustomBoneSet[bString]);
                                                        }
                                                        catch (Exception e)
                                                        {
                                                            FlexibleMessageBox.Show("Error reading bone data.\n\nBone: " + bString +
                                                                                    "\n\n" + e.Message, "ImportModel Error " + Info.appVersion, MessageBoxButtons.OK, MessageBoxIcon.Error);
                                                            return;
                                                        }
                                                    }

                                                    cData.bIndex.Add(tempbIndex[a + 1]);
                                                }
                                                break;
											}
										}
									}
								}
							}
						}
					}
				}
				catch(Exception e)
				{
					FlexibleMessageBox.Show("Error reading .dae file.\n" + e.Message, "ImportModel Error " + Info.appVersion, MessageBoxButtons.OK, MessageBoxIcon.Error);
					return;
				}

                if(extraBones.Count > 0)
                {
                    if (importSettings[Strings.All].UseOriginalBones)
                    {
                        FlexibleMessageBox.Show("Bone Addition not allowed when using original bones.\n The import has been cancelled.", "ImportModel Error " + Info.appVersion, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    var boneString = "";
                    foreach( string bone in extraBones )
                    {
                        boneString += bone + " ";
                    }
                    FlexibleMessageBox.Show("Bones not originally in this item were detected; TexTools will attempt to add them to the item.\n Bone(s): " + boneString, "ImportModel Notification " + Info.appVersion, MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

                if (!importSettings[Strings.All].UseOriginalBones)
                {
                    // Rebuild BoneSet 0 with our custom bone listing.
                    modelData.BoneSet[0].BoneCount = CustomBoneSet.Count;
                    if(CustomBoneSet.Count > 64)
                    {
                        FlexibleMessageBox.Show("Item exceeds 64 Bone Limit.\nImport with 'Use Original Bones', or reduce bone count below 64.\n\nThe import has been cancelled.", "ImportModel Error " + Info.appVersion, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    int originalBonesLength = modelData.Bones.Count;
                    for (int i = 0; i < CustomBoneSet.Count; i++)
                    {
                        // Add the extra bones into the bonestrings list.
                        if (i >= modelData.Bones.Count)
                        {
                            modelData.Bones.Add(extraBones[i - originalBonesLength]);
                        }
                        modelData.BoneSet[0].BoneData[i] = i;
                    }
                }

                // For all imported meshes
                 for (int i = 0; i < pDict.Count; i++)
                 {
                    var mDict = pDict[i];


                    for (int j = 0; j < mDict.Count; j++)
					{
						if (mDict.ContainsKey(j))
						{
							if (mDict[j].texCoord.Count < 1)
							{
								FlexibleMessageBox.Show("TexTools detected missing Texture Coordinates for:\nMesh: " + i + " Part: " + j
									+ "\n\nPlease check your model before importing again.", "ImportModel Error " + Info.appVersion, MessageBoxButtons.OK, MessageBoxIcon.Error);
								return;
							}

							if (mDict[j].weights.Count < 1)
							{
								FlexibleMessageBox.Show("TexTools detected missing Bone Weight Data for:\nMesh: " + i + " Part: " + j
									+ "\n\nPlease check your model before importing again.", "ImportModel Error " + Info.appVersion, MessageBoxButtons.OK, MessageBoxIcon.Error);
								return;

							}

							if (mDict[j].bIndex.Count < 1)
							{
								FlexibleMessageBox.Show("TexTools detected missing Bone Index Data for:\nMesh: " + i + " Part: " + j
									+ "\n\nPlease check your model before importing again.", "ImportModel Error " + Info.appVersion, MessageBoxButtons.OK, MessageBoxIcon.Error);
								return;
							}
						}
					}
				}

				for (int i = 0; i < pDict.Count; i++)
				{
					var mDict = pDict[i];

					var lastIndex = 0;
					List<int> bInList = new List<int>();

					int c = 0;
					int vMax = 0;
					int nMax = 0;
					int tcMax = 0;
					int tc2Max = 0;
					int bnMax = 0;
                    int vcMax = 0;
                    int vaMax = 0;

                    if (mDict.Count > 0)
					{
						for (int j = 0; j < mDict.Count; j++)
						{

                            try
						    {
						        while (!mDict.ContainsKey(c))
						        {
						            cdDict[i].partsDict.Add(c, 0);
						            c++;
                                }

                                /* Error Checking */
                                int numVerts = mDict[c].vIndexList.Count;
                                int maxVert = numVerts == 0 ? 0 : mDict[c].vIndexList.Max();

                                int numNormals = mDict[c].nIndexList.Count;
                                int maxNormal = numNormals == 0 ? 0 : mDict[c].nIndexList.Max();

                                int numTexCoord = mDict[c].tcIndexList.Count;
                                int maxTexCoord = numTexCoord == 0 ? 0 : mDict[c].tcIndexList.Max();

                                int numTexCoord2 = mDict[c].tc2IndexList.Count;
                                int maxTexCoord2 = numTexCoord2 == 0 ? 0 : mDict[c].tc2IndexList.Max();

                                int numBinormals = mDict[c].bnIndexList.Count;
                                int maxBinormal = numBinormals == 0 ? 0 : mDict[c].bnIndexList.Max();

                                int numVertColors = mDict[c].vcIndexList.Count;
                                int maxVertColor = numVertColors == 0 ? 0 : mDict[c].vcIndexList.Max();

                                int numVertAlphas = mDict[c].vaIndexList.Count;
                                int maxVertAlpha = numVertAlphas == 0 ? 0 : mDict[c].vaIndexList.Max();

                                if (numVerts != numNormals // Normals are simple.
                                    || (numVerts != numTexCoord ) // Check if our coordinate count matches
                                    || (numVerts != numTexCoord2 ) // Check if our coordinate2 count matches
                                    || (numVerts != numVertColors)
                                    || (numVerts != numVertAlphas))
                                {
                                    FlexibleMessageBox.Show("Number of data elements did not match for the following mesh part:\nMesh: " + i + " Part: " + j
                                        + "\n\nThis has a chance of either crashing TexTools or causing other errors in the import\n" +
                                        "\nVertexCount: " + numVerts + 
                                        "\nNormal Count:" + numNormals + 
                                        "\nUV1 Coordinates: " + numTexCoord + 
                                        "\nUV2 Coordinates: " + numTexCoord2 +
                                        "\nVertex Colors: " + numVertColors +
                                        "\nUV3 Coordinates (Vertex Alphas): " + numVertAlphas +
                                        "\n\nThe import will now attempt to continue.", "ImportModel Warning " + Info.appVersion, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                }

                                if(maxVert > mDict[c].vertex.Count())
                                {
                                    FlexibleMessageBox.Show("The following mesh part references Vertices which do not exist in the file:\nMesh: " + i + " Part: " + j
                                        + "\n\nThis has a chance of either crashing TexTools or causing other errors in the import."
                                        + "\n\nThe import will now attempt to continue.", "ImportModel Warning " + Info.appVersion, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                }

                                if (maxNormal > mDict[c].normal.Count())
                                {
                                    FlexibleMessageBox.Show("The following mesh part references Normals which do not exist in the file:\nMesh: " + i + " Part: " + j
                                        + "\n\nThis has a chance of either crashing TexTools or causing other errors in the import."
                                        + "\n\nThe import will now attempt to continue.", "ImportModel Warning " + Info.appVersion, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                }

                                if (maxTexCoord > mDict[c].texCoord.Count())
                                {
                                    FlexibleMessageBox.Show("The following mesh part references UV1 Data which does not exist in the file:\nMesh: " + i + " Part: " + j
                                        + "\n\nThis has a chance of either crashing TexTools or causing other errors in the import."
                                        + "\n\nThe import will now attempt to continue.", "ImportModel Warning " + Info.appVersion, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                }

                                if (maxTexCoord2 > mDict[c].texCoord2.Count())
                                {
                                    FlexibleMessageBox.Show("The following mesh part references UV2 Data which does not exist in the file:\nMesh: " + i + " Part: " + j
                                        + "\n\nThis has a chance of either crashing TexTools or causing other errors in the import."
                                        + "\n\nThe import will now attempt to continue.", "ImportModel Warning " + Info.appVersion, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                }

                                if (maxVertColor > mDict[c].vertexColors.Count())
                                {
                                    FlexibleMessageBox.Show("The following mesh part references Vertex Color Data which does not exist in the file:\nMesh: " + i + " Part: " + j
                                        + "\n\nThis has a chance of either crashing TexTools or causing other errors in the import."
                                        + "\n\nThe import will now attempt to continue.", "ImportModel Warning " + Info.appVersion, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                }

                                if (maxVertAlpha > mDict[c].vertexAlphas.Count())
                                {
                                    FlexibleMessageBox.Show("The following mesh part references Vertex Alpha(UV3) Data which does not exist in the file:\nMesh: " + i + " Part: " + j
                                        + "\n\nThis has a chance of either crashing TexTools or causing other errors in the import."
                                        + "\n\nThe import will now attempt to continue.", "ImportModel Warning " + Info.appVersion, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                }




                                // All meshes should have data for all fields at this point.
                                // Either the DAE had valid data, or we dummied it up.

                                // If the data lengths were mismatched, we at least threw a warning.

                                // Appropriate amount of values is 
                                // ( [ Highest reference from indices ] + 1 )* [ entries per index ] ) + 

                                cdDict[i].vertex.AddRange(mDict[c].vertex.Take((mDict[c].vIndexList.Max() + 1) * 3 ));
						        cdDict[i].normal.AddRange(mDict[c].normal.Take((mDict[c].nIndexList.Max() + 1) * 3));
						        cdDict[i].texCoord.AddRange(mDict[c].texCoord.Take((mDict[c].tcIndexList.Max() + 1) * tcStride ));
						        cdDict[i].texCoord2.AddRange(mDict[c].texCoord2.Take((mDict[c].tc2IndexList.Max() + 1) * tcStride ));
						        cdDict[i].tangent.AddRange(mDict[c].tangent.Take((mDict[c].bnIndexList.Max() + 1) * 3));
						        cdDict[i].biNormal.AddRange(mDict[c].biNormal.Take((mDict[c].bnIndexList.Max() + 1) * 3));
                                cdDict[i].vertexColors.AddRange(mDict[c].vertexColors.Take((mDict[c].vcIndexList.Max() + 1) * 3));
                                cdDict[i].vertexAlphas.AddRange(mDict[c].vertexAlphas.Take((mDict[c].vaIndexList.Max() + 1) * tcStride ));

                                // Rebuild the index list.
                                for (int k = 0; k < mDict[c].vIndexList.Count; k++)
						        {
						            cdDict[i].index.Add(mDict[c].vIndexList[k] + vMax);     // 0
						            cdDict[i].index.Add(mDict[c].nIndexList[k] + nMax);     // 1
						            cdDict[i].index.Add(mDict[c].tcIndexList[k] + tcMax);   // 2
						            cdDict[i].index.Add(mDict[c].tc2IndexList[k] + tc2Max); // 3
						            cdDict[i].index.Add(mDict[c].bnIndexList[k] + bnMax);   // 4
                                    cdDict[i].index.Add(mDict[c].vcIndexList[k] + vcMax);   // 5
                                    cdDict[i].index.Add(mDict[c].vaIndexList[k] + vaMax);   // 6
                                }


                                cdDict[i].partsDict.Add(c, mDict[c].vIndexList.Count);
                                mDict[c].indexStride = 7;

                                vMax += mDict[c].vIndexList.Max() + 1;
						        nMax += mDict[c].nIndexList.Max() + 1;
						        tcMax += mDict[c].tcIndexList.Max() + 1;
                                tc2Max += mDict[c].tc2IndexList.Max() + 1;
                                bnMax += mDict[c].bnIndexList.Max() + 1;
                                vcMax += mDict[c].vcIndexList.Max() + 1;
                                vaMax += mDict[c].vaIndexList.Max() + 1;

                                // If there are varied index strides between parts in a mesh
                                // You're pretty much fucked.
                                // But that's why the data is dummied up earlier in the code.
                                // Theoretically all parts should have a stride of 7(?) here.
                                cdDict[i].indexStride = mDict[c].indexStride;


						        cdDict[i].weights.AddRange(mDict[c].weights);
						        cdDict[i].vCount.AddRange(mDict[c].vCount);

						        if (j > 0)
						        {

						            lastIndex = bInList.Max() + 1;

						            for (int a = 0; a < mDict[c].bIndex.Count; a += 2)
						            {
						                cdDict[i].bIndex.Add(mDict[c].bIndex[a]);
						                cdDict[i].bIndex.Add(mDict[c].bIndex[a + 1] + lastIndex);
						                bInList.Add(mDict[c].bIndex[a + 1] + lastIndex);
						            }
						        }
						        else
						        {
						            for (int a = 0; a < mDict[c].bIndex.Count; a += 2)
						            {
						                cdDict[i].bIndex.Add(mDict[c].bIndex[a]);
						                cdDict[i].bIndex.Add(mDict[c].bIndex[a + 1]);
						                bInList.Add(mDict[c].bIndex[a + 1]);
						            }
						        }

						        c++;
						    }
						    catch (Exception e)
						    {
						        FlexibleMessageBox.Show("There was an error reading data imported data at:" +
						                                "\n\nMesh: " + i + " Part: " + c + "\n\n" + e.Message, "Data Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                return;
						    }

						}
					}
					else
					{
						cdDict[i].partsDict.Add(c, 0);
					}


				}

				List<ColladaMeshData> cmdList = new List<ColladaMeshData>();

				int m = 0;

                Dictionary<int, Dictionary<int, int>> majorCorrections = new Dictionary<int, Dictionary<int, int>>();

                int minorCorrections = 0;
                foreach (var cd in cdDict.Values)
				{
                    majorCorrections.Add(m, new Dictionary<int, int>());

					ColladaMeshData cmd = new ColladaMeshData();

					Vector3Collection Vertex = new Vector3Collection(cd.vertex.Count / 3);
					Vector2Collection TexCoord = new Vector2Collection(cd.texCoord.Count / tcStride);
					Vector2Collection TexCoord2 = new Vector2Collection(cd.texCoord2.Count / tcStride);
					Vector3Collection Normals = new Vector3Collection(cd.normal.Count / 3);
                    Vector3Collection VertexColors = new Vector3Collection(cd.vertexColors.Count / 3);
                    Vector2Collection VertexAlphas= new Vector2Collection(cd.vertexAlphas.Count / tcStride);

                    IntCollection Indices = new IntCollection();
					List<byte[]> blendIndices = new List<byte[]>();
					List<byte[]> blendWeights = new List<byte[]>();
					List<string> boneStringList = new List<string>();


					Vector3Collection nVertex = new Vector3Collection(cd.vertex.Count / 3);
					Vector2Collection nTexCoord = new Vector2Collection(cd.texCoord.Count / tcStride);
					Vector2Collection nTexCoord2 = new Vector2Collection(cd.texCoord2.Count / tcStride);
					Vector3Collection nNormals = new Vector3Collection(cd.normal.Count / 3);
                    Vector3Collection nVertexColors = new Vector3Collection(cd.vertexColors.Count / 3);
                    Vector2Collection nVertexAlphas = new Vector2Collection(cd.vertexAlphas.Count / tcStride);

                    List<byte[]> nBlendIndices = new List<byte[]>();
					List<byte[]> nBlendWeights = new List<byte[]>();

					for (int i = 0; i < cd.vertex.Count; i += 3)
					{
						Vertex.Add(new SharpDX.Vector3((cd.vertex[i] / Info.modelMultiplier), (cd.vertex[i + 1] / Info.modelMultiplier), (cd.vertex[i + 2] / Info.modelMultiplier)));
					}

					for (int i = 0; i < cd.normal.Count; i += 3)
					{
						Normals.Add(new SharpDX.Vector3(cd.normal[i], cd.normal[i + 1], cd.normal[i + 2]));
					}

                    for (int i = 0; i < cd.vertexColors.Count; i += 3)
                    {
                        VertexColors.Add(new SharpDX.Vector3(cd.vertexColors[i], cd.vertexColors[i + 1], cd.vertexColors[i + 2]));
                    }

                    for (int i = 0; i < cd.vertexAlphas.Count; i += tcStride)
                    {
                        VertexAlphas.Add(new SharpDX.Vector2(cd.vertexAlphas[i], cd.vertexAlphas[i + 1]));
                    }

                    for (int i = 0; i < cd.texCoord.Count; i += tcStride)
					{
						TexCoord.Add(new SharpDX.Vector2(cd.texCoord[i], cd.texCoord[i + 1]));
					}

					for (int i = 0; i < cd.texCoord2.Count; i += tcStride)
					{
						TexCoord2.Add(new SharpDX.Vector2(cd.texCoord2[i], cd.texCoord2[i + 1]));
					}

					int vTrack = 0;

					try
					{
						for (int i = 0; i < Vertex.Count; i++)
						{
							int bCount = cd.vCount[i];

							int boneSum = 0;

							List<byte> biList = new List<byte>();
							List<byte> bwList = new List<byte>();

							int newbCount = 0;
							for (int j = 0; j < bCount * 2; j += 2)
							{
								var b = cd.bIndex[vTrack * 2 + j];
								var bi = (byte)b;
								var bw = (byte)Math.Round(cd.weights[cd.bIndex[vTrack * 2 + j + 1]] * 255f);

								if (bw != 0)
								{
									biList.Add(bi);
									bwList.Add(bw);
									boneSum += bw;
									newbCount++;
								}

							}
							int originalbCount = bCount;
							bCount = newbCount;

							if (bCount < 4)
							{
								int remainder = 4 - bCount;

								for (int k = 0; k < remainder; k++)
								{
									biList.Add(0);
									bwList.Add(0);
								}
							}
							else if (bCount > 4)
							{
								int extras = bCount - 4;

								for (int k = 0; k < extras; k++)
								{
									var min = bwList.Min();
									var minIndex = bwList.IndexOf(min);
									int count = (bwList.Count(x => x == min));
									bwList.Remove(min);
									biList.RemoveAt(minIndex);
									boneSum -= min;
								}
							}

							if (boneSum != 255)
							{
								int diff = boneSum - 255;
								var max = bwList.Max();
								var maxIndex = bwList.IndexOf(max);

                                if(Math.Abs(diff) == 1)
                                {
                                    minorCorrections++;
                                } else
                                {
                                    majorCorrections[m].Add(i, diff);
                                }

								if (diff < 0)
								{
									bwList[maxIndex] += (byte)Math.Abs(diff);
								}
								else
								{
									// Subtract difference when over-weight.
									bwList[maxIndex] -= (byte)Math.Abs(diff);
								}
							}

							boneSum = 0;
							bwList.ForEach(x => boneSum += x);

							blendIndices.Add(biList.ToArray());
							blendWeights.Add(bwList.ToArray());
							vTrack += originalbCount;
						}
					}
					catch
					{
					   FlexibleMessageBox.Show("An error occured while trying to read the .DAE file's weight data. \nThe import has been canceled.", "Mesh Import Warning",MessageBoxButtons.OK,MessageBoxIcon.Warning);
                        return;
					}



					var extraVertDict = modelData.LoD[0].MeshList[m].extraVertDict;


					List<int> handedness = new List<int>();


					List<int[]> iList = new List<int[]>();
                    
					var indexMax = 0;


                    // Slice the rebuilt merged index list into per-triangle-index groupings.
					for(int i = 0; i < cd.index.Count; i += cd.indexStride)
					{
						iList.Add(cd.index.GetRange(i, cd.indexStride).ToArray());
					}

					if(cd.index.Count > 0)
					{
						indexMax = cd.index.Max();
					}

                    // Index Dictionary; This essentially becomes our
                    // Final index list.  [Index Order, Index Id Pointer]
                    var indexDict = new Dictionary<int, int>(iList.Count / 2);  // Start these allocated with a reasonable amount of data
                    var uniquesList = new List<int[]>(iList.Count / 2);         // To avoid constant reallocation.
                    var uniqueCount = 0;

					for (int i = 0; i < iList.Count; i++)
					{
					    try
					    {
                            var targetIndex = uniqueCount;
                            var listEntry = iList[i];

                            // Scan the entries we've already made to see if any match us.
                            for(var z = 0; z < uniqueCount; z++)
                            {
                                var targetEntry = uniquesList[z];

                                // We only really care about matching on position, normal, and UV1/2
                                if(listEntry[0] == targetEntry[0]
                                    && listEntry[1] == targetEntry[1]
                                    && listEntry[2] == targetEntry[2]
                                    && listEntry[3] == targetEntry[3])
                                {
                                    targetIndex = z;
                                    break;
                                }
                            }


                            // We didn't find a suitable index to match with, so we have to add 
                            // the data in and claim a new index number.
                            if (targetIndex == uniqueCount)
                            {
                                // All data should be available at this point,
                                // regardless of original source.
                                
                                nVertex.Add(Vertex[listEntry[0]]);
                                nBlendIndices.Add(blendIndices[listEntry[0]]);
                                nBlendWeights.Add(blendWeights[listEntry[0]]);
                                nNormals.Add(Normals[listEntry[1]]);
                                nTexCoord.Add(TexCoord[listEntry[2]]);
                                nTexCoord2.Add(TexCoord2[listEntry[3]]);
                                nVertexColors.Add(VertexColors[listEntry[5]]);
                                nVertexAlphas.Add(VertexAlphas[listEntry[6]]);
                                uniquesList.Add(listEntry);
                                uniqueCount++;
                            }

                            indexDict.Add(i, targetIndex);
					    }
					    catch (Exception e)
					    {
					        FlexibleMessageBox.Show("There was an error reindexing the data at:" +
					                                "\n\nIndex: " + i + "\n\n" + e.Message, "Reindexing Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
					        return;
                        }

					}

					HashSet<int> nVertList = new HashSet<int>();

					Indices.Clear();
                    Indices = new IntCollection(indexDict.Values);

					if (importSettings != null && importSettings.ContainsKey(m.ToString()))
					{
						if (importSettings[Strings.All].Fix || importSettings[m.ToString()].Fix)
						{
							foreach (var ev in extraVertDict)
							{
								var a = 0;
								foreach (var v in nVertex)
								{
									bool found = false;
									if (Vector3.NearEqual(ev.Value, v, new Vector3(0.02f)))
									{
										for (int i = 0; i < Indices.Count; i++)
										{
											if (a == Indices[i] && !nVertList.Contains(i) && !nVertDict.ContainsKey(ev.Key))
											{
												nVertDict.Add(ev.Key, i);
												nVertList.Add(i);
												found = true;
												break;

											}

										}

										if (found)
										{
											break;
										}
									}
									a++;
								}
							}
						}
					}

					MeshGeometry3D mg = new MeshGeometry3D
					{
						Positions = nVertex,
						Indices = Indices,
						Normals = nNormals,
						TextureCoordinates = nTexCoord
					};

					try
					{
						MeshBuilder.ComputeTangents(mg);
					}
					catch (Exception e)
					{
						FlexibleMessageBox.Show("Error computing tangents.\n\n" + e.Message, "ImportModel Error " + Info.appVersion, MessageBoxButtons.OK, MessageBoxIcon.Error);
						return;
					}


					SharpDX.Vector3[] tangents = new SharpDX.Vector3[nVertex.Count];
					SharpDX.Vector3[] bitangents = new SharpDX.Vector3[nVertex.Count];
					for (int a = 0; a < nVertex.Count; a += 3)
					{
						int idx1 = Indices[a];
						int idx2 = Indices[a + 1];
						int idx3 = Indices[a + 2];
						SharpDX.Vector3 vert1 = nVertex[idx1];
						SharpDX.Vector3 vert2 = nVertex[idx2];
						SharpDX.Vector3 vert3 = nVertex[idx3];
						SharpDX.Vector2 uv1 = nTexCoord[idx1];
						SharpDX.Vector2 uv2 = nTexCoord[idx2];
						SharpDX.Vector2 uv3 = nTexCoord[idx3];
						float deltaX1 = vert2.X - vert1.X;
						float deltaX2 = vert3.X - vert1.X;
						float deltaY1 = vert2.Y - vert1.Y;
						float deltaY2 = vert3.Y - vert1.Y;
						float deltaZ1 = vert2.Z - vert1.Z;
						float deltaZ2 = vert3.Z - vert1.Z;
						float deltaU1 = uv2.X - uv1.X;
						float deltaU2 = uv3.X - uv1.X;
						float deltaV1 = uv2.Y - uv1.Y;
						float deltaV2 = uv3.Y - uv1.Y;
						float r = 1.0f / (deltaU1 * deltaV2 - deltaU2 * deltaV1);
						SharpDX.Vector3 uDir = new SharpDX.Vector3((deltaV2 * deltaX1 - deltaV1 * deltaX2) * r, (deltaV2 * deltaY1 - deltaV1 * deltaY2) * r, (deltaV2 * deltaZ1 - deltaV1 * deltaZ2) * r);
						SharpDX.Vector3 vDir = new SharpDX.Vector3((deltaU1 * deltaX2 - deltaU2 * deltaX1) * r, (deltaU1 * deltaY2 - deltaU2 * deltaY1) * r, (deltaU1 * deltaZ2 - deltaU2 * deltaZ1) * r);
						tangents[idx1] += uDir;
						tangents[idx2] += uDir;
						tangents[idx3] += uDir;
						bitangents[idx1] += vDir;
						bitangents[idx2] += vDir;
						bitangents[idx3] += vDir;
					}

					float d;
					SharpDX.Vector3 tmpt;
					for (int a = 0; a < nVertex.Count; ++a)
					{
						SharpDX.Vector3 n = SharpDX.Vector3.Normalize(nNormals[a]);
						SharpDX.Vector3 t = SharpDX.Vector3.Normalize(tangents[a]);
						d = (SharpDX.Vector3.Dot(SharpDX.Vector3.Cross(n, t), bitangents[a]) < 0.0f) ? -1.0f : 1.0f;
						tmpt = new SharpDX.Vector3(t.X, t.Y, t.Z);
						mg.BiTangents.Add(tmpt);
						cmd.handedness.Add((int)d);
					}
					cmd.meshGeometry = mg;
					cmd.blendIndices = nBlendIndices;
					cmd.blendWeights = nBlendWeights;
					cmd.partsDict = cd.partsDict;
					cmd.texCoord2 = nTexCoord2;
                    cmd.vertexColors = nVertexColors;

                    // Go ahead and distill this down into just the single value we care about.
                    foreach (var uv3Coordinate in nVertexAlphas)
                    {
                        cmd.vertexAlphas.Add(uv3Coordinate.X);
                    }

					cmdList.Add(cmd);

					m++;
				}

                int totalCorrections = minorCorrections;
                foreach(var dict in majorCorrections)
                {
                    totalCorrections += dict.Value.Count;
                }


                if (totalCorrections > 0)
                {
                    string errorString = "Textools automatically adjusted bone weights on the following vertices.";

                    if (minorCorrections > 0)
                    {
                        errorString += "\n\n\t" + minorCorrections + " Vertices - Minor Weight Corrections(+/- 1)";
                    }
                    
                    for(int i = 0; i < majorCorrections.Count; i++)
                    {
                        if(majorCorrections[i].Count > 0)
                        {
                            errorString += "\n\nMesh #" + i + " Major Weight Corrections:\n";
                            foreach (var er in majorCorrections[i])
                            {
                                errorString += "\n\tVertex: " + er.Key + "\tCorrection: " + er.Value;
                            }

                        }
                    }


                    FlexibleMessageBox.Show(errorString, "Weight Corrections", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }


                // This is a fix for correcting for SE meshes where higher LoDs have fewer mesh groups.
                for (int l = 1; l < modelData.LoD.Count; l++)
                {
                    while (modelData.LoD[l].MeshList.Count < modelData.LoD[0].MeshList.Count)
                    {
                        var mesh = new Mesh();
                        var newPart = new MeshPart();
                        mesh.MeshPartList = new List<MeshPart>();
                        mesh.MeshPartList.Add(newPart);
                        modelData.LoD[l].MeshList.Add(mesh);
                    }
                }
                modelData.LoD[0].MeshCount = modelData.LoD[1].MeshCount = modelData.LoD[2].MeshCount = modelData.LoD[0].MeshList.Count;

                Create(cmdList, internalPath, selectedMesh, category, itemName, modelData);
			}
		}

		public static void Create(List<ColladaMeshData> cmdList, string internalPath, string selectedMesh, string category, string itemName, ModelData modelData)
		{
			var type = Helper.GetCategoryType(category);

			int lineNum = 0;
			bool inModList = false;
			JsonEntry modEntry = null;
			var extraIndexData = modelData.ExtraData;

			List<byte> mdlImport = new List<byte>();

			try
			{
				using (StreamReader sr = new StreamReader(Properties.Settings.Default.Modlist_Directory))
				{
					string line;
					while ((line = sr.ReadLine()) != null)
					{
						modEntry = JsonConvert.DeserializeObject<JsonEntry>(line);
						if (modEntry.fullPath.Equals(internalPath))
						{
							inModList = true;
							break;
						}
						lineNum++;
					}
				}
			}
			catch (Exception ex)
			{
				FlexibleMessageBox.Show("Error Accessing .modlist File \n" + ex.Message, "ImportModel Error " + Info.appVersion, MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
			/*
			* Imported Vertex Data Start
			*/

			Dictionary<int, ImportData> importDict = new Dictionary<int, ImportData>();


            int importMeshNum = 0;
			foreach(var cmd in cmdList)
			{
				ImportData id = new ImportData();
				int bc = 0;
				var mg = cmd.meshGeometry;

				for (int i = 0; i < mg.Positions.Count; i++)
				{
					if (type.Equals("weapon") || type.Equals("monster"))
					{
						var hx = System.Half.Parse(mg.Positions[i].X.ToString());
						id.dataSet1.AddRange(System.Half.GetBytes(hx));

						var hy = System.Half.Parse(mg.Positions[i].Y.ToString());
						id.dataSet1.AddRange(System.Half.GetBytes(hy));

						var hz = System.Half.Parse(mg.Positions[i].Z.ToString());
						id.dataSet1.AddRange(System.Half.GetBytes(hz));

						id.dataSet1.AddRange(BitConverter.GetBytes((short)15360));
					}
					else
					{
						//vertex points
						id.dataSet1.AddRange(BitConverter.GetBytes(mg.Positions[i].X));
						id.dataSet1.AddRange(BitConverter.GetBytes(mg.Positions[i].Y));
						id.dataSet1.AddRange(BitConverter.GetBytes(mg.Positions[i].Z));
					}

					//blend weight
					foreach (var bw in cmd.blendWeights[bc])
					{
						id.dataSet1.Add(bw);
					}


					//blend index
					foreach(var bi in cmd.blendIndices[bc])
					{
						id.dataSet1.Add(bi);
					}

					//bc += 4;
					bc++;
				}

				for (int i = 0; i < mg.Normals.Count; i++)
				{
					//Normal X
					float nX = mg.Normals[i].X;
					id.dataSet2.AddRange(BitConverter.GetBytes(nX));

					//Normal Y
					float nY = mg.Normals[i].Y;
					id.dataSet2.AddRange(BitConverter.GetBytes(nY));

					//Normal Z
					float nZ = mg.Normals[i].Z;
					id.dataSet2.AddRange(BitConverter.GetBytes(nZ));

					var btn = mg.BiTangents[i];
					var h = cmd.handedness[i];
					if (h > 0) { btn = SharpDX.Vector3.Normalize(-btn); }
					int c = id.dataSet2.Count;

					//tangent X
					if (btn.X < 0) { id.dataSet2.Add((byte)((Math.Abs(btn.X) * 255 + 255) / 2)); }
					else { id.dataSet2.Add((byte)((-Math.Abs(btn.X) - .014) * 255 / 2 - 255 / 2)); }

					//tangent Y
					if (btn.Y < 0) { id.dataSet2.Add((byte)((Math.Abs(btn.Y) * 255 + 255) / 2)); }
					else { id.dataSet2.Add((byte)((-Math.Abs(btn.Y) - .014) * 255 / 2 - 255 / 2)); }

					//tangent Z
					if (btn.Z < 0) { id.dataSet2.Add((byte)((Math.Abs(btn.Z) * 255 + 255) / 2)); }
					else { id.dataSet2.Add((byte)((-Math.Abs(btn.Z) - .014) * 255 / 2 - 255 / 2)); }

					//tangent W
					byte tw = 0;
					if(h == 1)
					{
						tw = 255;
					}
					else if (h == -1)
					{
						tw = 0;
					}

					id.dataSet2.Add(tw);

                    //Color
                    byte r = Convert.ToByte(Math.Round(cmd.vertexColors[i].X * 255));
                    byte g = Convert.ToByte(Math.Round(cmd.vertexColors[i].Y * 255));
                    byte b = Convert.ToByte(Math.Round(cmd.vertexColors[i].Z * 255));
                    byte a = Convert.ToByte(Math.Round(cmd.vertexAlphas[i] * 255));
                    id.dataSet2.Add(r);
                    id.dataSet2.Add(g);
                    id.dataSet2.Add(b);
                    id.dataSet2.Add(a);

                    //TexCoord X
                    float x = mg.TextureCoordinates[i].X;
					id.dataSet2.AddRange(BitConverter.GetBytes(x));

					//TexCoord Y
					float y = mg.TextureCoordinates[i].Y * -1;
					id.dataSet2.AddRange(BitConverter.GetBytes(y));

					float z = 0f;
					float w = 0f;

					if (cmd.texCoord2.Count > 0)
					{
						//TexCoord2 X
						z = cmd.texCoord2[i].X;

						//TexCoord2 Y
						w = cmd.texCoord2[i].Y * -1;
					}

					id.dataSet2.AddRange(BitConverter.GetBytes(z));
					id.dataSet2.AddRange(BitConverter.GetBytes(w));

					//id.dataSet2.AddRange(BitConverter.GetBytes((short)0));
					//id.dataSet2.AddRange(BitConverter.GetBytes((short)15360));
				}

				foreach (var i in mg.Indices)
				{
                    // Don't allow overflow.
                    if(i > 65535)
                    {
                        FlexibleMessageBox.Show("Mesh group " + importMeshNum +" exceeded maximum allowable complexity.\n" +
                            "Please split the mesh across multiple mesh groups.\n" +
                            "\nMaximum unqiue indices: 65,535 " +
                            "\nUnique indices in mesh: " + mg.Indices.Max() + 
                            "\n\nThe import has been cancelled.", "ImportModel Error " + Info.appVersion, MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

					id.indexSet.AddRange(BitConverter.GetBytes((ushort)i));
				}

				importDict.Add(importMeshNum, id);
				importMeshNum++;
			}

			/*
			* Imported Vertex Data End
			*/


			/*
			 * Open oringial MDL file
			 */
			int offset;

			if (inModList)
			{
				offset = modEntry.originalOffset;
			}
			else
			{
				var MDLFile = Path.GetFileName(internalPath);
				var MDLFolder = internalPath.Substring(0, internalPath.LastIndexOf("/"));

				offset = Helper.GetDataOffset(FFCRC.GetHash(MDLFolder), FFCRC.GetHash(MDLFile), Strings.ItemsDat);
			}


			int datNum = ((offset / 8) & 0x000f) / 2;

			var MDLDatData = Helper.GetType3DecompressedData(offset, datNum, Strings.ItemsDat);

			List<byte> compressedData = new List<byte>();
			List<byte> datHeader = new List<byte>();

			using (BinaryReader br = new BinaryReader(new MemoryStream(MDLDatData.Item1)))
			{
				//skip blank header
				br.BaseStream.Seek(68, SeekOrigin.Begin);

				/* 
				 * -------------------------------
				 * Vertex Info Block Start
				 * -------------------------------
				 */
				List<byte> vertexInfoBlock = new List<byte>();
				int compVertexInfoSize;

				//vertex info section

				//changed texcoordinates to Float2

				int vertexInfoSize = 136;

                // Copy the first vertex info block
                byte[] firstVertexInfo = br.ReadBytes(vertexInfoSize);

                // Read off all the rest of the vertex info blocks
                for (int i = 1; i < MDLDatData.Item2; i++)
                {
                    br.ReadBytes(vertexInfoSize);
                }

                // Rewrite the vertex info blocks for the appropriate number of them
                int meshCount = 0;
                for(int i = 0; i < modelData.LoD.Count; i++)
                {
                    // Use LoD 0 values since we're cloning that data.
                    meshCount += modelData.LoD[0].MeshList.Count;
                }

                for (int i = 0; i < meshCount; i++)
                {
                    vertexInfoBlock.AddRange(firstVertexInfo);
                }

                /* 
				 * -------------------------------
				 * Vertex Info Block End
				 * -------------------------------
				 */


                /* 
				 * -------------------------------
				 * Model Data Block Start
				 * -------------------------------
				 */
                List<byte> modelDataBlock = new List<byte>();
				int compModelDataSize = 0;

                //number of strings (int)
                int oldStringCount = br.ReadInt32();

                //string block size (int)
                int originalStringBlockSize = br.ReadInt32();

                //string block
                var originalStringBlock = br.ReadBytes(originalStringBlockSize);

                //unknown (int)
                int unknownInt = br.ReadInt32();
                byte[] unknownIntBytes = BitConverter.GetBytes(unknownInt);
                //unknownInt = 0;

				//mesh count (short)
				short oldMeshCount = br.ReadInt16();

				//num of atr strings (short)
				short oldAtrStringCount = br.ReadInt16();

				//num of mesh parts (short)
                //modelData.LoD[0].
				short oldMeshPartCount = br.ReadInt16();

				//num of material strings (short)
				short oldMatStringCount = br.ReadInt16();

				//num of bone strings (short)
				short oldBoneStringCount = br.ReadInt16();

				//bone list count (short)
				short boneListCount = br.ReadInt16();


                // # Of Mesh groups with hiding data?
                // # Of independent mesh hiding instances?
                short MeshHidingDataCount = br.ReadInt16();

                // # of Mesh hiding parts?
                short unk2 = br.ReadInt16();

                // # of Mesh hiding verts?
                short unk3 = br.ReadInt16();

                // Seems to always be 1027
                short unk4 = br.ReadInt16();

                // ??? Almost always 0 
                short unk5 = br.ReadInt16();

                // ??? Almost always 0 
                short unk6 = br.ReadInt16();

                // ??? Almost always 0 
                short unk7 = br.ReadInt16();

                // ??? Almost always 0 
                short unk8 = br.ReadInt16();

                // ??? Almost always 0 
                short unk9 = br.ReadInt16();

                // ??? Almost always 0 
                short unk10 = br.ReadInt16();

                // ??? Almost always 0 
                short unk11 = br.ReadInt16();

                // ??? Almost always 0 
                short unk12 = br.ReadInt16();



                // Parse the string block out.
                List<string> materials = new List<string>();
                List<string> oldMaterials = new List<string>();
                List<string> oldBones = new List<string>();
                List<string> bones = new List<string>();
                List<string> attributes = new List<string>();
                List<string> oldAttributes = new List<string>();
                List<string> extraStringBlockData = new List<string>();

                using (BinaryReader br1 = new BinaryReader(new MemoryStream(originalStringBlock)))
                {
                    br1.BaseStream.Seek(0, SeekOrigin.Begin);
                    int pos = 0;

                    // Attributes
                    for (int i = 0; i < oldAtrStringCount; i++)
                    {
                        byte b;
                        List<byte> name = new List<byte>();
                        while ((b = br1.ReadByte()) != 0)
                        {
                            pos++;
                            name.Add(b);
                        }

                        pos++;
                        string atrName = Encoding.ASCII.GetString(name.ToArray());
                        atrName = atrName.Replace("\0", "");

                        attributes.Add(atrName);
                    }

                    // Bones
                    for (int i = 0; i < oldBoneStringCount; i++)
                    {
                        byte b;
                        List<byte> name = new List<byte>();
                        while ((b = br1.ReadByte()) != 0)
                        {
                            pos++;
                            name.Add(b);
                        }

                        pos++;
                        string boneName = Encoding.ASCII.GetString(name.ToArray());
                        boneName = boneName.Replace("\0", "");

                        bones.Add(boneName);
                    }

                    // Materials
                    for (int i = 0; i < oldMatStringCount; i++)
                    {
                        byte b;
                        List<byte> name = new List<byte>();
                        while ((b = br1.ReadByte()) != 0)
                        {
                            pos++;
                            name.Add(b);
                        }

                        pos++;
                        string material = Encoding.ASCII.GetString(name.ToArray());
                        material = material.Replace("\0", "");

                        materials.Add(material);
                    }

                    // Finish reading off the rest of the data and store it for later.
                    while (pos < originalStringBlockSize)
                    {
                        byte b;
                        List<byte> name = new List<byte>();
                        while ((b = br1.ReadByte()) != 0)
                        {
                            pos++;
                            name.Add(b);
                        }

                        pos++;
                        string extraName = Encoding.ASCII.GetString(name.ToArray());
                        extraName = extraName.Replace("\0", "");

                        if (extraName != "")
                        {
                            extraStringBlockData.Add(extraName);
                        }
                        pos++;
                    }

                }


                short newMeshHidingDataCount =  MeshHidingDataCount;
                
                short newUnk2 = newMeshHidingDataCount == (short) 0 ? (short) 0 : unk2;
                short newUnk3 = newMeshHidingDataCount == (short) 0 ? (short) 0 : unk3;
                

                // Pull in the model data options.
                oldBones = bones;
                bones = modelData.Bones;

                oldMaterials = materials;
                materials = modelData.Materials;

                oldAttributes = attributes;
                attributes = modelData.Attributes;

                // Make sure we're using new counts
                short newBoneStringCount = (short)bones.Count;
                short newAtrStringCount = (short)attributes.Count;
                short newMatStringCount = (short)materials.Count;

                // Write the string block data back together with our modification.
                List<byte> stringBlockBytes = new List<byte>();
                for (int i = 0; i < attributes.Count; i++)
                {
                    stringBlockBytes.AddRange(Encoding.ASCII.GetBytes(attributes[i]));
                    stringBlockBytes.Add(0);
                }
                for (int i = 0; i < bones.Count; i++)
                {
                    stringBlockBytes.AddRange(Encoding.ASCII.GetBytes(bones[i]));
                    stringBlockBytes.Add(0);
                }
                for (int i = 0; i < materials.Count; i++)
                {
                    stringBlockBytes.AddRange(Encoding.ASCII.GetBytes(materials[i]));
                    stringBlockBytes.Add(0);
                }

                for (int i = 0; i < extraStringBlockData.Count; i++)
                {
                    stringBlockBytes.AddRange(Encoding.ASCII.GetBytes(extraStringBlockData[i]));
                    stringBlockBytes.Add(0);
                }

                stringBlockBytes.Add(0);

                short newMeshCount = (short)(modelData.LoD[0].MeshList.Count() * 3);

                short newMeshPartCount = 0;
                for (int lodIndex = 0; lodIndex < modelData.LoD.Count; lodIndex++ )
                {
                    for (int meshIndex = 0; meshIndex < modelData.LoD[lodIndex].MeshList.Count; meshIndex++)
                    {
                        newMeshPartCount+= (short)(modelData.LoD[lodIndex].MeshList[meshIndex].MeshPartList.Count());
                    }
                }

                int meshPartDelta = 0;
                int meshDelta = 0;
                if (newMeshCount != oldMeshCount)
                {
                    meshDelta = newMeshCount - oldMeshCount;
                }


                if (newMeshPartCount != oldMeshPartCount)
                {
                    meshPartDelta = newMeshPartCount - oldMeshPartCount;
                }

                int newStringCount = newAtrStringCount + newBoneStringCount + newMatStringCount + extraStringBlockData.Count;

                // Finally ready to push all the data into the binary block.
                modelDataBlock.AddRange(BitConverter.GetBytes(newStringCount));
                modelDataBlock.AddRange(BitConverter.GetBytes(stringBlockBytes.Count));
                modelDataBlock.AddRange(stringBlockBytes);
                modelDataBlock.AddRange(BitConverter.GetBytes(unknownInt));
                modelDataBlock.AddRange(BitConverter.GetBytes(newMeshCount));
                modelDataBlock.AddRange(BitConverter.GetBytes(newAtrStringCount));
                modelDataBlock.AddRange(BitConverter.GetBytes(newMeshPartCount));
                modelDataBlock.AddRange(BitConverter.GetBytes(newMatStringCount));
                modelDataBlock.AddRange(BitConverter.GetBytes(newBoneStringCount));
                modelDataBlock.AddRange(BitConverter.GetBytes(boneListCount));
                modelDataBlock.AddRange(BitConverter.GetBytes(newMeshHidingDataCount));
                modelDataBlock.AddRange(BitConverter.GetBytes(newUnk2));

                // Hang onto this for later, we'll inject a corrected number.
                int unk3Location = modelDataBlock.Count; 

                modelDataBlock.AddRange(BitConverter.GetBytes(newUnk3));
                modelDataBlock.AddRange(BitConverter.GetBytes(unk4));
                modelDataBlock.AddRange(BitConverter.GetBytes(unk5));
                modelDataBlock.AddRange(BitConverter.GetBytes(unk6));
                modelDataBlock.AddRange(BitConverter.GetBytes(unk7));
                modelDataBlock.AddRange(BitConverter.GetBytes(unk8));
                modelDataBlock.AddRange(BitConverter.GetBytes(unk9));
                modelDataBlock.AddRange(BitConverter.GetBytes(unk10));
                modelDataBlock.AddRange(BitConverter.GetBytes(unk11));
                modelDataBlock.AddRange(BitConverter.GetBytes(unk12));

				//Unknown 16 bytes - Seems to always be 0 x 16
				byte[] UnknownBytes = br.ReadBytes(16);
                modelDataBlock.AddRange(new byte[16]);

                int[] LoDChunkInfoOffset = new int[3];
                int[] LoDVertexInfoOffset = new int[3];


                if (unk5 > 0)
				{
					modelDataBlock.AddRange(br.ReadBytes(unk5 * 32));
				}

                int vertexDataSize1 = 0;
                int vertexDataSize2 = 0;


                #region LoD Header Information
                //LoD Section
                List<LevelOfDetail> lodList = new List<LevelOfDetail>();
				List<LevelOfDetail> OriginalLodList = new List<LevelOfDetail>();
                int meshOffset = 0;

				for (int i = 0; i < 3; i++)
				{
					LevelOfDetail lod = new LevelOfDetail()
					{
						MeshOffset = br.ReadInt16(),
						MeshCount = br.ReadInt16(),
					};

					OriginalLodList.Add(new LevelOfDetail()
					{
						MeshOffset = lod.MeshOffset,
						MeshCount = lod.MeshCount
					});

                    // Recalculate mesh counts and offsets.
                    lod.MeshCount = modelData.LoD[0].MeshList.Count; // Use LoD 0 since we're cloning LoD 0's data.
                    lod.MeshOffset = meshOffset;
                    meshOffset += lod.MeshCount;
                    

					List<byte> LoDChunk = new List<byte>();
                    //LoD UNK
                    byte[] LoDUnknown1 = br.ReadBytes(28);
                    byte[] oldLodOffset = br.ReadBytes(4); // Offset is seek-written later.
                    byte[] LoDUnknown2 = br.ReadBytes(8);

                    // These unknowns seem to have no impact on the game.
                    // Replace with dead bytes and see if it has some impact eventually?
                    LoDChunk.AddRange(new byte[36]);

					int oldVertBufferSize = br.ReadInt32();
					OriginalLodList[i].VertexDataSize = oldVertBufferSize;
					int vertSize = 0;
                    vertexDataSize1 = 20;
                    vertexDataSize2 = 36;

					if (type.Equals("weapon") || type.Equals("monster"))
					{
                        vertexDataSize1 = 16;
					}

					for(int m = 0; m < cmdList.Count; m++)
					{
						var mg = cmdList[m].meshGeometry;

						if (extraIndexData.totalExtraCounts != null && extraIndexData.totalExtraCounts.ContainsKey(m)
                            && newMeshHidingDataCount > 0)
						{
							vertSize += ((mg.Positions.Count + extraIndexData.totalExtraCounts[m]) * vertexDataSize1) + ((mg.Positions.Count + extraIndexData.totalExtraCounts[m]) * vertexDataSize2);
						}
						else
						{
							vertSize += (mg.Positions.Count * vertexDataSize1) + (mg.Positions.Count * vertexDataSize2);
						}
					}

					lod.VertexDataSize = vertSize;

					//LoD Index Buffer Size (int)
					int oldIndexBufferSize = br.ReadInt32();
					OriginalLodList[i].IndexDataSize = oldIndexBufferSize;


					int idSize = 0;
					foreach(var cmd in cmdList)
					{
						var mg = cmd.meshGeometry;

						int pad = (16 - ((mg.Indices.Count * 2) % 16));
						if(pad == 16)
						{
							pad = 0;
						}

						idSize += (mg.Indices.Count * 2) + pad;
					}

					lod.IndexDataSize = idSize;

                    //LoD Vertex Offset (int)
                    var originalVertexOffset = br.ReadInt32();
                    OriginalLodList[i].VertexOffset = originalVertexOffset;

                    // Placeholder for recalculation later.
                    lod.VertexOffset = 0;

                    //LoD Index Offset (int)
                    var originalIndexOffset = br.ReadInt32();
                    OriginalLodList[i].IndexOffset = originalIndexOffset;

                    lod.IndexOffset = 0;
                    LoDChunk.InsertRange(28, BitConverter.GetBytes(lod.IndexOffset));

                    //LoD Mesh Offset (short)
                    modelDataBlock.AddRange(BitConverter.GetBytes((short)lod.MeshOffset));

					//LoD Mesh Count (short)
					modelDataBlock.AddRange(BitConverter.GetBytes((short)lod.MeshCount));

                    //LoD Chunk 
                    LoDChunkInfoOffset[i] = modelDataBlock.Count + 28;
					modelDataBlock.AddRange(LoDChunk.ToArray());

                    //LoD Vetex Buffer Size (int)
					modelDataBlock.AddRange(BitConverter.GetBytes(lod.VertexDataSize));

                    //LoD Index Buffer Size (int)
                    modelDataBlock.AddRange(BitConverter.GetBytes(lod.IndexDataSize));

                    //LoD Vertex Offset (int)
                    LoDVertexInfoOffset[i] = modelDataBlock.Count();
                    modelDataBlock.AddRange(BitConverter.GetBytes(lod.VertexOffset));

					//LoD Index Offset (int)
					modelDataBlock.AddRange(BitConverter.GetBytes(lod.IndexOffset));

					lodList.Add(lod);
				}
                #endregion


                #region VertexInfo Compression/Finalization
                //Replace Half with Floats and compress VertexInfo data
                int lodStructOffset = 0;
                for (int i = 0; i < lodList.Count; i++)
                {
                    for (int x = 0; x < lodList[i].MeshCount; x++)
                    {
                        var normType = (lodStructOffset) + 26;
                        var bnOffset = (lodStructOffset) + 33;
                        var clrOffset = (lodStructOffset) + 41;
                        var tcOffset = (lodStructOffset) + 49;
                        var tcType = (lodStructOffset) + 50;

                        vertexInfoBlock.RemoveAt(normType);
                        vertexInfoBlock.Insert(normType, 2);

                        vertexInfoBlock.RemoveAt(bnOffset);
                        vertexInfoBlock.Insert(bnOffset, 12);

                        vertexInfoBlock.RemoveAt(clrOffset);
                        vertexInfoBlock.Insert(clrOffset, 16);

                        vertexInfoBlock.RemoveAt(tcOffset);
                        vertexInfoBlock.Insert(tcOffset, 20);

                        vertexInfoBlock.RemoveAt(tcType);
                        vertexInfoBlock.Insert(tcType, 3);

                        lodStructOffset += 136;
                    }
				}

				var compVertexInfo = Compressor(vertexInfoBlock.ToArray());
				compressedData.AddRange(BitConverter.GetBytes(16));
				compressedData.AddRange(BitConverter.GetBytes(0));
				compressedData.AddRange(BitConverter.GetBytes(compVertexInfo.Length));
				compressedData.AddRange(BitConverter.GetBytes(vertexInfoBlock.Count));
				compressedData.AddRange(compVertexInfo);

				var padding = 128 - ((compVertexInfo.Length + 16) % 128);

				compressedData.AddRange(new byte[padding]);
				compVertexInfoSize = compVertexInfo.Length + 16 + padding;
                #endregion


                #region Mesh Header Information
                //Meshes
                Dictionary<int, List<MeshInfo>> meshInfoDict = new Dictionary<int, List<MeshInfo>>();
                short meshPartOffset = 0;

				for(int i = 0; i < lodList.Count; i++)
				{
					List<MeshInfo> meshInfoList = new List<MeshInfo>();

					for (int j = 0; j < lodList[i].MeshCount; j++)
					{

                        MeshInfo meshInfo;

                        // If we're on an old mesh, read in all the data from file.
                        if (j < OriginalLodList[i].MeshCount)
                        {
                            OriginalLodList[i].MeshList.Add(new Mesh());

                            meshInfo = new MeshInfo()
                            {
                                VertexCount = br.ReadInt32(),
                                IndexCount = br.ReadInt32(),
                                MaterialNum = br.ReadInt16(),
                                MeshPartOffset = br.ReadInt16(),
                                MeshPartCount = br.ReadInt16(),
                                BoneListIndex = br.ReadInt16(),
                                IndexDataOffset = br.ReadInt32(),
                                VertexDataOffsets = new List<int> { br.ReadInt32(), br.ReadInt32(), br.ReadInt32() },
                                VertexSizes = new List<int> { br.ReadByte(), br.ReadByte(), br.ReadByte() },
                                VertexDataBlockCount = br.ReadByte()
                            };

                            OriginalLodList[i].MeshList[j].MeshInfo = new MeshInfo()
                            {
                                VertexCount = meshInfo.VertexCount,
                                IndexCount = meshInfo.IndexCount,
                                IndexDataOffset = meshInfo.IndexDataOffset,
                                VertexDataOffsets = new List<int>(meshInfo.VertexDataOffsets),
                                VertexSizes = new List<int>(meshInfo.VertexSizes),
                                MeshPartCount = meshInfo.MeshPartCount,
                                MeshPartOffset = meshInfo.MeshPartOffset
                            };

                            if(!importSettings[Strings.All].UseOriginalBones)
                            {
                                meshInfo.BoneListIndex = 0;
                            }
                        } else
                        {
                            // Don't allow mesh addition in LoD 0 with 'Use Original Bones'
                            if (importSettings[Strings.All].UseOriginalBones && i == 0)
                            {
                                MessageBox.Show("Mesh Addition is not allowed when using Original Bones.\n\nThe import has been canceled.");
                                return;
                            }

                            // New Mesh, just create a default one.
                            meshInfo = new MeshInfo();

                            // Clone the vertex sizing data from the base mesh in this LoD level.
                            meshInfo.VertexSizes = new List<int>(meshInfoList[0].VertexSizes);
                            meshInfo.VertexDataOffsets = new List<int>(meshInfoList[0].VertexDataOffsets);

                            // Pretty sure this is always 2?
                            meshInfo.VertexDataBlockCount = meshInfoList[0].VertexDataBlockCount;

                        }

                        lodList[i].MeshList.Add(modelData.LoD[i].MeshList[j]);
                        lodList[i].MeshList[j].MeshInfo = meshInfo;


                        // Assign material from our import data.
                        meshInfo.MaterialNum = modelData.LoD[i].MeshList[j].MaterialId;

                        // Use the updated count from the user's file.
                        meshInfo.MeshPartCount = modelData.LoD[i].MeshList[j].MeshPartList.Count;
                        meshInfo.MeshPartOffset = meshPartOffset;
                        meshPartOffset += (short) meshInfo.MeshPartCount;
                        
                        // Pull over the mesh part count the user specified
                        // Should only really be applicable to LoD 0, but best to be consistent.
                        meshInfo.MeshPartCount = modelData.LoD[i].MeshList[j].MeshPartList.Count();

                        meshInfo.VertexSizes[0] = vertexDataSize1;
                        meshInfo.VertexSizes[1] = vertexDataSize2;

						try
						{
							var mg = cmdList[j].meshGeometry;
							int vc;
							//Vertex Count (int)
							if (extraIndexData.totalExtraCounts != null && extraIndexData.totalExtraCounts.ContainsKey(j)
                                && newMeshHidingDataCount > 0)
							{

								vc = mg.Positions.Count + extraIndexData.totalExtraCounts[j];

								modelDataBlock.AddRange(BitConverter.GetBytes(vc));
							}
							else
							{
								vc = mg.Positions.Count;
								modelDataBlock.AddRange(BitConverter.GetBytes(vc));
							}
							meshInfo.VertexCount = vc;

							//Index Count (int)
							modelDataBlock.AddRange(BitConverter.GetBytes(mg.Indices.Count));
							meshInfo.IndexCount = mg.Indices.Count;
						}
						catch
						{
							modelDataBlock.AddRange(BitConverter.GetBytes(0));
							modelDataBlock.AddRange(BitConverter.GetBytes(0));
							meshInfo.VertexCount = 0;
							meshInfo.IndexCount = 0;
						}

						//material index (short)
						modelDataBlock.AddRange(BitConverter.GetBytes((short)meshInfo.MaterialNum));

                        // mesh part table offset (short)
						modelDataBlock.AddRange(BitConverter.GetBytes((short)meshInfo.MeshPartOffset));

						//mesh part count (short)
						modelDataBlock.AddRange(BitConverter.GetBytes((short)meshInfo.MeshPartCount));

						//bone list index (short)
						modelDataBlock.AddRange(BitConverter.GetBytes((short)meshInfo.BoneListIndex));

						if(j != 0)
						{
                            // Mesh Groups other than 0
							//index data offset (int)
							int meshIndexPadding = 8 - (meshInfoList[j - 1].IndexCount % 8);
							if (meshIndexPadding == 8)
							{
								meshIndexPadding = 0;
							}
							meshInfo.IndexDataOffset = meshInfoList[j - 1].IndexDataOffset + meshInfoList[j - 1].IndexCount + meshIndexPadding;
							modelDataBlock.AddRange(BitConverter.GetBytes(meshInfo.IndexDataOffset));

						}
						else
						{
                            // Mesh Group 0
							//index data offset (int)
							modelDataBlock.AddRange(BitConverter.GetBytes(meshInfo.IndexDataOffset));
						}

						int posCount = 0;
						try
						{
							var mg = cmdList[j].meshGeometry;
							if (extraIndexData.totalExtraCounts != null && extraIndexData.totalExtraCounts.ContainsKey(j)
                                && newMeshHidingDataCount > 0)
							{
								posCount = mg.Positions.Count + extraIndexData.totalExtraCounts[j];
							}
							else
							{
								posCount = mg.Positions.Count;
							}
						}
						catch
						{
							posCount = 0;
						}

                        // Mesh Group not-Zero
						if(j != 0)
						{
							//vertex data offset[0] (int)
							meshInfo.VertexDataOffsets[0] = meshInfoList[j - 1].VertexDataOffsets[1] + (meshInfoList[j - 1].VertexCount * meshInfoList[j - 1].VertexSizes[1]);
							modelDataBlock.AddRange(BitConverter.GetBytes(meshInfo.VertexDataOffsets[0]));

							//vertex data offset[1] (int)
							meshInfo.VertexDataOffsets[1] = meshInfo.VertexDataOffsets[0] + (meshInfo.VertexCount * meshInfo.VertexSizes[0]);
							modelDataBlock.AddRange(BitConverter.GetBytes(meshInfo.VertexDataOffsets[1]));

							//vertex data offset[2] (int)
							modelDataBlock.AddRange(BitConverter.GetBytes(meshInfo.VertexDataOffsets[2]));
						}
                        // Mesh Group 0
						else
						{
							//vertex data offset[0] (int)
							modelDataBlock.AddRange(BitConverter.GetBytes(meshInfo.VertexDataOffsets[0]));

							//vertex data offset[1] (int)
							meshInfo.VertexDataOffsets[1] = posCount * meshInfo.VertexSizes[0];
							modelDataBlock.AddRange(BitConverter.GetBytes(meshInfo.VertexDataOffsets[1]));

							//vertex data offset[2] (int)
							modelDataBlock.AddRange(BitConverter.GetBytes(meshInfo.VertexDataOffsets[2]));
						}

						//vertex data size[0] (byte)
						modelDataBlock.Add((byte)meshInfo.VertexSizes[0]);
						//vertex data size[1] (byte)
						modelDataBlock.Add((byte)meshInfo.VertexSizes[1]);
						//vertex data size[2] (byte)
						modelDataBlock.Add((byte)meshInfo.VertexSizes[2]);

						//Data block count (byte)
						modelDataBlock.Add((byte)meshInfo.VertexDataBlockCount);

						meshInfoList.Add(meshInfo);
					}
					meshInfoDict.Add(i, meshInfoList);

				}

                #endregion


                #region Attribute Offsets
                // Attribute string offsets
                byte[] oldAttrBytes = br.ReadBytes(oldAtrStringCount * 4);
                int runningStringBlockOffset = 0;

                List<int> newAttrBytes = new List<int>();
                for(byte i = 0; i < newAtrStringCount; i++)
                {
                    int oldValue = 0;
                    int newValue = runningStringBlockOffset;
                    if (i < oldAtrStringCount)
                    {
                        byte[] bytes = new byte[4];
                        bytes[0] = oldAttrBytes[i * 4];
                        bytes[1] = oldAttrBytes[(i * 4) + 1];
                        bytes[2] = oldAttrBytes[(i * 4) + 2];
                        bytes[3] = oldAttrBytes[(i * 4) + 3];
                        oldValue = BitConverter.ToInt32(bytes, 0);
                    }

                    if(oldValue != 0 && oldValue != newValue)
                    {
                        MessageBox.Show(String.Format("Attribute Data mismatch: {0} : Old-{1} : New-Calculated-{2}", attributes[i], oldValue, newValue));
                    }
                    
                    modelDataBlock.AddRange(BitConverter.GetBytes(newValue));
                    newAttrBytes.Add(newValue);

                    // # of characters + a space at the end.
                    runningStringBlockOffset += attributes[i].Length + 1;
                }
                
                
				if(unk6 > 0)
				{
                    // Magical Unknown Data
					modelDataBlock.AddRange(br.ReadBytes(unk6 * 20));
				}
                #endregion


                #region Mesh Part Header Information
                //Mesh Parts
                List<MeshPart> meshPart = new List<MeshPart>();

				int meshPadd = 0;
                short lastBoneOffset = 0;
                short lastBoneCount = 0;

				for (int l = 0; l < lodList.Count; l++)
				{
					for (int i = 0; i < meshInfoDict[l].Count; i++)
					{
						var mList = meshInfoDict[l];
						var mPartCount = mList[i].MeshPartCount;

                        int oldIndexOffset = 0;
                        int oldIndexCount = 0;
                        for (int j = 0; j < mPartCount; j++)
						{
							MeshPart mp = new MeshPart();

                            // == Index Offsets (int) ==

                            // Mesh Group 0
                            if (i == 0)
                            {
                                // Part Zero
                                if (j == 0)
                                {
                                    mp.IndexOffset = 0;
                                    // If we have an old mesh to read off of, use the data (It's always 0 anyways though)
                                    if (i < OriginalLodList[l].MeshList.Count())
                                    {
                                        oldIndexOffset = br.ReadInt32();
                                    }
                                    mp.IndexOffset = 0;
                                }
                                // Part Non-Zero
                                else
                                {
                                    // If we're still in the original Mesh Part listing, advance the seek cursor.
                                    if (i < OriginalLodList[l].MeshList.Count() && j < OriginalLodList[l].MeshList[i].MeshInfo.MeshPartCount)
                                    {
                                        oldIndexOffset = br.ReadInt32();
                                    }

                                    mp.IndexOffset = meshPart[meshPart.Count - 1].IndexOffset + meshPart[meshPart.Count - 1].IndexCount;
                                }
                            }
                            // Mesh Group > 1
                            else if (i > 0)
                            {
                                // If we're still in the original Mesh Part listing, advance the seek cursor.
                                if (i < OriginalLodList[l].MeshList.Count() && j < OriginalLodList[l].MeshList[i].MeshInfo.MeshPartCount)
                                {
                                    oldIndexOffset = br.ReadInt32();
                                }

                                // Part Zero
                                if (j == 0)
                                {
                                    int pad = i > 0 ? meshPadd : 0;
                                    mp.IndexOffset = meshPart[meshPart.Count - 1].IndexOffset + meshPart[meshPart.Count - 1].IndexCount + meshPadd;
                                }
                                // Part Non-Zero
                                else
                                {
                                    mp.IndexOffset = meshPart[meshPart.Count - 1].IndexOffset + meshPart[meshPart.Count - 1].IndexCount;
                                }
                            }

                            modelDataBlock.AddRange(BitConverter.GetBytes(mp.IndexOffset));

                            // == Index Counts (int) ==

                            // Mesh is in the imported DAE
                            if (i < cmdList.Count)
							{
								var partsDict = cmdList[i].partsDict;

                                // If we're still in the original Mesh Part listing, advance the seek cursor.
                                if (i < OriginalLodList[l].MeshList.Count() && j < OriginalLodList[l].MeshList[i].MeshInfo.MeshPartCount)
                                {
                                    oldIndexCount = br.ReadInt32();
                                }


								if (partsDict.ContainsKey(j))
								{
									mp.IndexCount = partsDict[j];
								}
								else
								{
									mp.IndexCount = 0;
								}
							}
                            // Mesh was omitted from the imported DAE.
							else if (i >= cmdList.Count)
                            {
                                // If we're still in the original Mesh Part listing, advance the seek cursor.
                                if (i < OriginalLodList[l].MeshList.Count() && j < OriginalLodList[l].MeshList[i].MeshInfo.MeshPartCount)
                                {
                                    oldIndexCount = br.ReadInt32();
                                }

                                mp.IndexCount = 0;
                            }

                            modelDataBlock.AddRange(BitConverter.GetBytes(mp.IndexCount));


                            // If we're at the last part in a group, math out the padding.
                            if (j == mPartCount - 1)
							{
                                // Pad stuff
								var pad = ((mp.IndexOffset + mp.IndexCount) % 8);

								if(pad != 0)
								{
									meshPadd = 8 - pad;
								}
								else
								{
									meshPadd = 0;
								}

							}

                            //Attributes (int)
                            // If we're still in the original Mesh Part listing, advance the seek cursor.
                            int originalAttributes = 0;
                            try
                            {
                                if (i < OriginalLodList[l].MeshList.Count() && j < OriginalLodList[l].MeshList[i].MeshInfo.MeshPartCount)
                                {
                                     originalAttributes = br.ReadInt32();
                                }

                                // Pull attributes from our import data.
                                mp.Attributes = modelData.LoD[0].MeshList[i].MeshPartList[j].Attributes;
                            } catch(Exception e)
                            {
                                originalAttributes = 0;
                                mp.Attributes = 0;
                            }

                            // Make sure we can't be referencing attributes beyond the end of our attributes list.
                            //  It's a bitmask, where each bit references the attributes in order from the attribute strings
                            Int32 maxValue = 0;
                            if (newAtrStringCount > 0)
                            {
                                maxValue = (int) Math.Pow(2, newAtrStringCount);
                            }

                            if (maxValue == 0)
                            {
                                // If we have no attribute strings, we can't have attributes.
                                mp.Attributes = 0;
                                modelDataBlock.AddRange(BitConverter.GetBytes(mp.Attributes));
                            } else
                            {
                                mp.Attributes = mp.Attributes % maxValue;
                                modelDataBlock.AddRange(BitConverter.GetBytes(mp.Attributes));
                            }



                            #region Part Bone Counts and Bone Offsets
                            // == Bone reference offset (short) ==

                            short oldOffset = 0;

                            // If we're still in the original Mesh Part listing, advance the seek cursor.
                            if (i < OriginalLodList[l].MeshList.Count() && j < OriginalLodList[l].MeshList[i].MeshInfo.MeshPartCount)
                            {
                                oldOffset = br.ReadInt16();
                            }


                            mp.BoneOffset = lastBoneOffset + lastBoneCount;
							modelDataBlock.AddRange(BitConverter.GetBytes((short)mp.BoneOffset));

                            lastBoneOffset = (short)mp.BoneOffset;


                            // == Bone reference count (short) ==

                            short oldCount = 0;

                            // If we're still in the original Mesh Part listing, advance the seek cursor.
                            if (i < OriginalLodList[l].MeshList.Count() && j < OriginalLodList[l].MeshList[i].MeshInfo.MeshPartCount)
                            {
                                oldCount = br.ReadInt16();
                            }
                            
                            // Only allow bones in LoD 0
                            mp.BoneCount = modelData.Bones.Count;
                            modelDataBlock.AddRange(BitConverter.GetBytes((short)mp.BoneCount));
                            
                            lastBoneCount = (short) mp.BoneCount;
                            #endregion

                            meshPart.Add(mp);
						}
					}
				}
                #endregion


                #region Unknown 12
                if (unk12 > 0)
				{
					modelDataBlock.AddRange(br.ReadBytes(unk12 * 12));
				}
                #endregion


                #region Bone and Material String Offsets
                // == Bone & Material String Block Offsets == 


                // Move the read cursor up into the right place for later.

                for (int i = 0; i < oldMatStringCount; i++)
                {
                    br.ReadInt32();
                }

                for ( int i = 0; i < oldBoneStringCount; i++)
                {
                    br.ReadInt32();
                }

                // Build the new offset lists.

                List<int> boneStringBlockOffsets = new List<int>();
                for (int i = 0; i < newBoneStringCount; i++)
                {
                    int newValue = runningStringBlockOffset;
                    boneStringBlockOffsets.Add(newValue);

                    // # of characters + a space.
                    runningStringBlockOffset += bones[i].Length + 1;
                }

                List<int> materialStringBlockOffsets = new List<int>();
                for (int i = 0; i < newMatStringCount; i++)
                {
                    int newValue = runningStringBlockOffset;
                    materialStringBlockOffsets.Add(newValue);

                    // # of characters + a space.
                    runningStringBlockOffset += materials[i].Length + 1;
                }

                // Write the new offset lists.

                for (int i = 0; i < materialStringBlockOffsets.Count; i++)
                {
                    modelDataBlock.AddRange(BitConverter.GetBytes(materialStringBlockOffsets[i]));
                }

                for (int i = 0; i < boneStringBlockOffsets.Count; i++)
                {
                    modelDataBlock.AddRange(BitConverter.GetBytes(boneStringBlockOffsets[i]));
                }
                #endregion


                #region Bone Sets
                // Bone Sets - List of shorts which reference bones
                // Parts then reference the bone set list.
                for (int i = 0; i < boneListCount; i++)
				{
                    //bone list
                    List<short> boneSet = new List<short>();
                    for(int y = 0; y < 64; y++)
                    {
                        short boneNumber = br.ReadInt16();

                        if (importSettings[Strings.All].UseOriginalBones) {
                            boneSet.Add(boneNumber);
                        } else
                        {
                            // Just make all bone sets use all bones by default.
                            if (y < bones.Count)
                            {
                                boneSet.Add((short)y);
                            }
                            else
                            {
                                boneSet.Add((short)0);
                            }
                        }
                    }

                    for(int y = 0; y < 64; y++)
                    {
                        modelDataBlock.AddRange(BitConverter.GetBytes(boneSet[y]));
                    }

                    //bone count (int)
                    int oldBoneCount = br.ReadInt32();
                    if (importSettings[Strings.All].UseOriginalBones)
                    {
                        modelDataBlock.AddRange(BitConverter.GetBytes(oldBoneCount));
                    }
                    else
                    {
                        modelDataBlock.AddRange(BitConverter.GetBytes(bones.Count));
                    }
                }
                #endregion


                #region Mesh-Hiding Data
                Dictionary<int, int> nMax = new Dictionary<int, int>();
				Dictionary<int, int> nOffsets = new Dictionary<int, int>();
				for (int i = 0; i < cmdList.Count; i++)
				{
					if (cmdList[i].meshGeometry.Indices.Count > 0)
					{
						nMax.Add(i, cmdList[i].meshGeometry.Indices.Max());
					}
					else
					{
						nMax.Add(i, 0);
					}
					nOffsets.Add(i, meshInfoDict[0][i].IndexDataOffset);
				}


				Dictionary<int, int> indexLoc = new Dictionary<int, int>();
				Dictionary<int, int> indexMin = new Dictionary<int, int>();
				Dictionary<int, List<int>> extraIndices = new Dictionary<int, List<int>>();
				List<ExtraIndex> indexCounts = new List<ExtraIndex>();


                ushort pCount = 0;
                ushort pCount1 = 0;
                ushort pCount2 = 0;

                if (MeshHidingDataCount > 0)
                {
                    short totalLoD0MaskCount = 0;
                    for (int i = 0; i < MeshHidingDataCount; i++)
                    {
                        //not sure
                        byte[] unk1UnknownBytes = br.ReadBytes(4);


                        //LoD[0] Extra Data Index
                        var p1 = br.ReadUInt16();

                        //LoD[1] Extra Data Index
                        var p2 = br.ReadUInt16();

                        //LoD[2] Extra Data Index
                        var p3 = br.ReadUInt16();

                        //LoD[0] Extra Data Part Count
                        var p1n = br.ReadUInt16();
                        pCount += p1n;

                        //LoD[1] Extra Data Part Count
                        var p2n = br.ReadUInt16();
                        pCount1 += p2n;

                        //LoD[2] Extra Data Part Count
                        var p3n = br.ReadUInt16();
                        pCount2 += p3n;

                        if (i < newMeshHidingDataCount)
                        {
                            modelDataBlock.AddRange(unk1UnknownBytes);
                            modelDataBlock.AddRange(BitConverter.GetBytes(p1));
                            modelDataBlock.AddRange(BitConverter.GetBytes((ushort) 0));
                            modelDataBlock.AddRange(BitConverter.GetBytes((ushort) 0));
                            modelDataBlock.AddRange(BitConverter.GetBytes(p1n));
                            modelDataBlock.AddRange(BitConverter.GetBytes((ushort) 0));
                            modelDataBlock.AddRange(BitConverter.GetBytes((ushort) 0));
                        }

                    }

                    // Only scan the extra index data for the original items.
                    for (int i = 0; i < OriginalLodList[0].MeshCount; i++)
					{
						var ido = OriginalLodList[0].MeshList[i].MeshInfo.IndexDataOffset;
						indexLoc.Add(ido, i);
					}



                    List<int> maskCounts = new List<int>();
                    Dictionary<int, int> totalExtraCounts = new Dictionary<int, int>();
          
					for (int i = 0; i < pCount; i++)
					{
						//Index Offset Start
						var m1 = br.ReadInt32();
						var iLoc = indexLoc[m1];

						//index count
						var mCount = br.ReadInt32();

                        //index offset in unk3
                        var mOffset = br.ReadInt32();

                        indexCounts.Add(new ExtraIndex() { IndexLocation = iLoc, IndexCount = mCount });
                        maskCounts.Add(mCount);

                        if (newMeshHidingDataCount > 0)
                        {
                            modelDataBlock.AddRange(BitConverter.GetBytes(nOffsets[iLoc]));
                            modelDataBlock.AddRange(BitConverter.GetBytes(mCount));
                            modelDataBlock.AddRange(BitConverter.GetBytes(mOffset));
                        }

                    }

                    // Advance the seek pointer into the appropriate position.
                    // Don't need to write the data back in, since we set the 
                    // LoD Extra Mesh part counts to 0.
                    var remaining = br.ReadBytes((pCount1 + pCount2) * 12);

					for (int i = 0; i < pCount; i++)
					{
						totalLoD0MaskCount += (short) maskCounts[i];
					}

					var unk3Remainder = (unk3 * 4) - (totalLoD0MaskCount * 4);

					foreach (var ic in indexCounts)
					{
						HashSet<int> mIndexList = new HashSet<int>();
						
						for (int i = 0; i < ic.IndexCount; i++)
						{
							short[] unk3Indices = new short[2];
							//index its replacing? attatched to?
							var oIndex = br.ReadInt16();
							//extra index following last equipment index
							var mIndex = br.ReadInt16();

							if(importSettings != null && importSettings.ContainsKey(ic.IndexLocation.ToString()))
							{
                                //Disabling hiding for mesh
								if (importSettings[Strings.All].Disable || importSettings[ic.IndexLocation.ToString()].Disable)
								{
									unk3Indices[0] = 0;
									unk3Indices[1] = 0;
								}
                                //Fixing hiding for mesh
								else if(importSettings[Strings.All].Fix || importSettings[ic.IndexLocation.ToString()].Fix)
								{
                                    if (nVertDict.ContainsKey(oIndex))
									{
										unk3Indices[0] = (short)nVertDict[oIndex];
										unk3Indices[1] = mIndex;
									}
									else
									{
										unk3Indices[0] = 0;
										unk3Indices[1] = 0;
									}
								}
                                //No true parameters, using default
                                else
                                {
                                    unk3Indices[0] = oIndex;
                                    unk3Indices[1] = mIndex;
                                }
							}
                            //no import settings were present for mesh, using default
							else
							{
                                unk3Indices[0] = oIndex;
								unk3Indices[1] = mIndex;
							}

							mIndexList.Add(mIndex);

							if (extraIndices.ContainsKey(ic.IndexLocation))
							{
								extraIndices[ic.IndexLocation].Add(mIndex);
							}
							else
							{
								extraIndices.Add(ic.IndexLocation, new List<int>() { mIndex });
							}

							ic.IndexList.Add(unk3Indices);
						}

						if (totalExtraCounts.ContainsKey(ic.IndexLocation))
						{
							totalExtraCounts[ic.IndexLocation] += mIndexList.Count;
						}
						else
						{
							totalExtraCounts.Add(ic.IndexLocation, mIndexList.Count);
						}
					}

					foreach (var ei in extraIndices)
					{
						indexMin.Add(ei.Key, ei.Value.Min());
					}

                    var m = 0;
                    foreach (var ic in indexCounts)
                    {
                        var iMax = nMax[ic.IndexLocation];
                        var iMin = indexMin[ic.IndexLocation];

                        for (int i = 0; i < ic.IndexCount; i++)
                        {
                            var oIndex = ic.IndexList[i][0];

                            var mIndex = ic.IndexList[i][1];
                            short nIndex = 0;
                            if (mIndex != 0)
                            {
                                short sub = (short)(mIndex - iMin);
                                nIndex = (short)(sub + iMax + 1);
                            }


                            if (newMeshHidingDataCount > 0)
                            {
                                modelDataBlock.AddRange(BitConverter.GetBytes(oIndex));
                                modelDataBlock.AddRange(BitConverter.GetBytes(nIndex));
                            }
                        }
                        m++;
                    }

                    // Advance the seek pointer into the appropriate position.
                    // Don't need to write the data back in, since we set the 
                    // LoD Extra Mesh part counts to 0.
                    byte[] remainder = br.ReadBytes(unk3Remainder);

                    // Rewrite our Unk3 count to match our changed values.
                    byte[] newUnk2Bytes = BitConverter.GetBytes(pCount);
                    byte[] newUnk3Bytes = BitConverter.GetBytes(totalLoD0MaskCount);
                    for (int x = 0; x < newUnk3Bytes.Length; x++)
                    {
                        modelDataBlock[unk3Location + x] = newUnk3Bytes[x];
                    }
                    for (int x = 0; x < newUnk2Bytes.Length; x++)
                    {
                        modelDataBlock[unk3Location + x - 2] = newUnk2Bytes[x];
                    }

                // End of Mesh Hiding Block 1
				}
                #endregion


                #region Unused Data
                // Bone index count (int)
                // The game doesn't seem to actually use this data?

                int boneIndexCount = br.ReadInt32();
                int newBoneIndexCount = 0;
                modelDataBlock.AddRange(BitConverter.GetBytes(newBoneIndexCount));

                //Bone indices
                List <short> strangeShorts = new List<short>();

                for (int i = 0; i < boneIndexCount / 2; i++)
                {
                    short value = br.ReadInt16();
                    strangeShorts.Add(value);
                    if (i < newBoneIndexCount / 2) {
                        modelDataBlock.AddRange(BitConverter.GetBytes(value));
                    }
                }

				// Padding count - Does this padding value have any significance?
                // There doesn't seem to be any need for this data block to end on a certain
                // bit divisibility.

				byte paddingCount = br.ReadByte();
				br.ReadBytes(paddingCount);
                paddingCount = 3;
				modelDataBlock.Add(paddingCount);
                byte[] paddingBytes = new byte[paddingCount];
                for (int i = 0; i < paddingCount; i++)
                {
                    paddingBytes[i] = 254;
                }
				modelDataBlock.AddRange(paddingBytes);

                // Bounding Boxes - Seems unused by FFXIV client.
                // Possibly used for Occlusion Culling or somesuch?
                // Can just carry through the same ones for now to be safe.
                byte[] BoundingBoxes = br.ReadBytes(128);
				modelDataBlock.AddRange(BoundingBoxes);

                //Bone transforms - these seem to be unused by FFXIV.
                //Possibly file data that's just carried through in the engine for creators?

                modelDataBlock.AddRange(br.ReadBytes(oldBoneStringCount * 32));
                modelDataBlock.AddRange(new byte[newBoneStringCount * 32]);

                #endregion


                #region Offset Recalculation
                // Calculate LoD offsets here.
                for (int i = 0; i < 3; i++) {
                    
                    // Recalculate offsets now that the modelDataBlock and vertexInfoBlocks have been fully solved.
                    LevelOfDetail lod = lodList[i];

                    // LoD Vertex Offset (int)
                    if (i == 0)
                    {
                        // There are 68 blank(?) bytes at the start of the data header.
                        int calculatedOffset = modelDataBlock.Count + vertexInfoBlock.Count() + 68;
                        lod.VertexOffset = calculatedOffset;
                    }
                    else
                    {
                        lod.VertexOffset = lodList[i - 1].IndexOffset + lodList[i - 1].IndexDataSize;
                    }

                    // Rewrite our vertex Offset
                    byte[] bytes = BitConverter.GetBytes(lod.VertexOffset);
                    for (int j = 0; j < bytes.Count(); j++)
                    {
                        modelDataBlock[LoDVertexInfoOffset[i] + j] = bytes[j];
                    }

                    // LoD Index Offset (int)
                    // Reinserted into both the LoD chunk and after the Vertex Offsets in the main block.
                    lod.IndexOffset = lod.VertexOffset + lod.VertexDataSize;

                    // Rewrite our index offset
                    bytes = BitConverter.GetBytes(lod.IndexOffset);
                    for (int j = 0; j < bytes.Count(); j++)
                    {
                        modelDataBlock[LoDVertexInfoOffset[i] + j + 4] = bytes[j];
                        modelDataBlock[LoDChunkInfoOffset[i] + j] = bytes[j];
                    }

                    lodList[i] = lod;
                }

                #endregion


                #region ModelDataBlock Finalization/Compression
                List<int> compModelSizes = new List<int>();

                var modelDataParts = (int)Math.Ceiling(modelDataBlock.Count / 16000f);
                int[] MDPartCounts = new int[modelDataParts];
                int MDRemaining = modelDataBlock.Count;

                for (int i = 0; i < modelDataParts; i++)
                {
                    if (MDRemaining >= 16000)
                    {
                        MDPartCounts[i] = 16000;
                        MDRemaining -= 16000;
                    }
                    else
                    {
                        MDPartCounts[i] = MDRemaining;
                    }
                }


                for (int i = 0; i < modelDataParts; i++)
                {
                    var compModelData = Compressor(modelDataBlock.GetRange(i * 16000, MDPartCounts[i]).ToArray());

                    compressedData.AddRange(BitConverter.GetBytes(16));
                    compressedData.AddRange(BitConverter.GetBytes(0));
                    compressedData.AddRange(BitConverter.GetBytes(compModelData.Length));
                    compressedData.AddRange(BitConverter.GetBytes(MDPartCounts[i]));
                    compressedData.AddRange(compModelData);

                    padding = 128 - ((compModelData.Length + 16) % 128);

                    compressedData.AddRange(new byte[padding]);

                    compModelDataSize += compModelData.Length + 16 + padding;
                    compModelSizes.Add(compModelData.Length + 16 + padding);
                }

                /* 
				 * -------------------------------
				 * Model Data Block End
				 * -------------------------------
				 */
                #endregion


                #region Vertex Data Block Finalization/Compression
                /*
				 * ---------------------
				 * Vertex Data Start
				 * ---------------------
				*/
                List<int> compMeshSizes = new List<int>();
				List<int> compIndexSizes = new List<int>();

				List<DataBlocks> dbList = new List<DataBlocks>();

				if(extraIndexData.totalExtraCounts != null)
				{
					foreach (var ec in extraIndexData.totalExtraCounts)
					{
						var extraLoc = ec.Key;
						var extraVerts = ec.Value;
						var LoD0 = OriginalLodList[0];
						var meshInfo = LoD0.MeshList[extraLoc].MeshInfo;

						var baseVertsToRead = meshInfo.VertexCount - extraVerts;
						br.BaseStream.Seek(LoD0.VertexOffset + meshInfo.VertexDataOffsets[0], SeekOrigin.Begin);
						br.ReadBytes(baseVertsToRead * meshInfo.VertexSizes[0]);
						var maskVerts = br.ReadBytes(extraVerts * meshInfo.VertexSizes[0]);

                        if (newMeshHidingDataCount > 0)
                        {

                            if (type.Equals("weapon") || type.Equals("monster") || meshInfo.VertexSizes[0] != 20)
                            {
                                importDict[extraLoc].dataSet1.AddRange(maskVerts);
                            } else
                            {
                                // This Data is 
                                // 12 bytes of vertex position ( float x 3 )
                                // 4 bytes of bone weights ( byte x 4 )
                                // 4 bytes of bone indices ( byte x 4 )

                                // To *correctly* port this data, we need to identify 
                                // Which bone we used to reference,
                                // and then which new bone we should reference after
                                // altering the bone list.
                                for(var idx = 0; idx < extraVerts; idx++)
                                {
                                    var extraVertOffset = idx * meshInfo.VertexSizes[0];
                                    
                                    // The first 16 bytes are the same.
                                    importDict[extraLoc].dataSet1.AddRange(maskVerts.Skip(20 * idx).Take(16));

                                    // The bone indices may need to be changed...
                                    // This is probably too complex of a task currently 
                                    // to do in this update (1.9.8.5)
                                    for(var i = 0; i < 4; i++)
                                    {
                                        var oldBoneIndex = maskVerts[16 + i];
                                        importDict[extraLoc].dataSet1.Add(oldBoneIndex);
                                    }
                                }

                            }
                        }

						br.ReadBytes(baseVertsToRead * meshInfo.VertexSizes[1]);

						for (int j = 0; j < extraVerts; j++)
						{
							//Normals
							System.Half h1 = System.Half.ToHalf((ushort)br.ReadInt16());
							System.Half h2 = System.Half.ToHalf((ushort)br.ReadInt16());
							System.Half h3 = System.Half.ToHalf((ushort)br.ReadInt16());
							System.Half h4 = System.Half.ToHalf((ushort)br.ReadInt16());

							var x = HalfHelper.HalfToSingle(h1);
							var y = HalfHelper.HalfToSingle(h2);
							var z = HalfHelper.HalfToSingle(h3);
							var w = HalfHelper.HalfToSingle(h4);

                           if (newMeshHidingDataCount > 0)
                            {
                                importDict[extraLoc].dataSet2.AddRange(BitConverter.GetBytes(x));
                                importDict[extraLoc].dataSet2.AddRange(BitConverter.GetBytes(y));
                                importDict[extraLoc].dataSet2.AddRange(BitConverter.GetBytes(z));
                                importDict[extraLoc].dataSet2.AddRange(br.ReadBytes(4));
                                importDict[extraLoc].dataSet2.AddRange(br.ReadBytes(4));
                            }

                            //texCoord
                            h1 = System.Half.ToHalf((ushort)br.ReadInt16());
							h2 = System.Half.ToHalf((ushort)br.ReadInt16());
							h3 = System.Half.ToHalf((ushort)br.ReadInt16());
							h4 = System.Half.ToHalf((ushort)br.ReadInt16());

							x = h1;
							y = h2;
							z = h3;
							w = h4;

                            if (newMeshHidingDataCount > 0)
                            {
                                importDict[extraLoc].dataSet2.AddRange(BitConverter.GetBytes(x));
                                importDict[extraLoc].dataSet2.AddRange(BitConverter.GetBytes(y));
                                importDict[extraLoc].dataSet2.AddRange(BitConverter.GetBytes(z));
                                importDict[extraLoc].dataSet2.AddRange(BitConverter.GetBytes(w));
                            }
						}
					}
				}


                
                for (int i = 0; i < 3; i++)
                {
                    DataBlocks db = new DataBlocks();
                    for (int j = 0; j < importDict.Count; j++)
                    {
                        db.VertexDataBlock.AddRange(importDict[j].dataSet1);
                        db.VertexDataBlock.AddRange(importDict[j].dataSet2);
                        db.IndexDataBlock.AddRange(importDict[j].indexSet);

                        var indexPadd = importDict[j].indexSet.Count % 16;
                        if (indexPadd != 0)
                        {
                            db.IndexDataBlock.AddRange(new byte[16 - indexPadd]);
                        }
                    }

                    db.VDBParts = (int)Math.Ceiling(db.VertexDataBlock.Count / 16000f);
                    int[] VDB1PartCounts = new int[db.VDBParts];
                    int VDB1Remaining = db.VertexDataBlock.Count;

                    for (int j = 0; j < db.VDBParts; j++)
                    {

                        if (VDB1Remaining >= 16000)
                        {
                            VDB1PartCounts[j] = 16000;
                            VDB1Remaining -= 16000;
                        }
                        else
                        {
                            VDB1PartCounts[j] = VDB1Remaining;
                        }
                    }

                    for (int j = 0; j < db.VDBParts; j++)
                    {
                        var compVertexData1 = Compressor(db.VertexDataBlock.GetRange(j * 16000, VDB1PartCounts[j]).ToArray());

                        compressedData.AddRange(BitConverter.GetBytes(16));
                        compressedData.AddRange(BitConverter.GetBytes(0));
                        compressedData.AddRange(BitConverter.GetBytes(compVertexData1.Length));
                        compressedData.AddRange(BitConverter.GetBytes(VDB1PartCounts[j]));
                        compressedData.AddRange(compVertexData1);

                        var vertexPadding = 128 - ((compVertexData1.Length + 16) % 128);

                        compressedData.AddRange(new byte[vertexPadding]);

                        db.compVertexDataBlockSize += compVertexData1.Length + 16 + vertexPadding;
                        compMeshSizes.Add(compVertexData1.Length + 16 + vertexPadding);
                    }


                    db.IDBParts = (int)Math.Ceiling(db.IndexDataBlock.Count / 16000f);
                    int[] IDB1PartCounts = new int[db.IDBParts];
                    int IDB1Remaining = db.IndexDataBlock.Count;

                    for (int j = 0; j < db.IDBParts; j++)
                    {

                        if (IDB1Remaining >= 16000)
                        {
                            IDB1PartCounts[j] = 16000;
                            IDB1Remaining -= 16000;
                        }
                        else
                        {
                            IDB1PartCounts[j] = IDB1Remaining;
                        }
                    }

                    var indexPadding = 0;

                    for (int j = 0; j < db.IDBParts; j++)
                    {
                        var compIndexData1 = Compressor(db.IndexDataBlock.GetRange(j * 16000, IDB1PartCounts[j]).ToArray());

                        compressedData.AddRange(BitConverter.GetBytes(16));
                        compressedData.AddRange(BitConverter.GetBytes(0));
                        compressedData.AddRange(BitConverter.GetBytes(compIndexData1.Length));
                        compressedData.AddRange(BitConverter.GetBytes(IDB1PartCounts[j]));
                        compressedData.AddRange(compIndexData1);

                        indexPadding = 128 - ((compIndexData1.Length + 16) % 128);

                        compressedData.AddRange(new byte[indexPadding]);

                        db.compIndexDataBlockSize += compIndexData1.Length + 16 + indexPadding;
                        compIndexSizes.Add(compIndexData1.Length + 16 + indexPadding);
                    }

                    dbList.Add(db);

                }
                for (int i = 0; i < dbList.Count; i++)
                {
                    for(int j = 0; j < dbList[i].IndexDataBlock.Count; j++)
                    {
                        if (dbList[i].IndexDataBlock[j] != dbList[0].IndexDataBlock[j])
                        {
                            MessageBox.Show("Err");
                        }

                    }
                }
                #endregion


                /*
				 * -----------------------------------
				 * Create Header Start
				 * -----------------------------------
				 */
                #region Header Generation
                int headerLength = 256;

                int blockCount = (compMeshSizes.Count + modelDataParts + 3 + compIndexSizes.Count);

                if (blockCount > 24)
                {
                    // Base size at count * 24 is technically only 252
                    // but having a 4 byte safety buffer is good.
                    int remainingBlocks = blockCount - 24;
                    int bytesUsed = remainingBlocks * 2;
                    int extensionsNeeded = (bytesUsed / 128) + 1;
                    int newSize = 256 + (extensionsNeeded * 128);
                    headerLength = newSize;
                }

                //Header Length
                datHeader.AddRange(BitConverter.GetBytes(headerLength));
				//Data Type
				datHeader.AddRange(BitConverter.GetBytes(3));
				//Uncompressed Size
				var uncompSize = vertexInfoBlock.Count + modelDataBlock.Count + 68;

				foreach(var dataBlock in dbList)
				{
					uncompSize += dataBlock.VertexDataBlock.Count + dataBlock.IndexDataBlock.Count;
				}

				datHeader.AddRange(BitConverter.GetBytes(uncompSize));
				//Max Buffer Size?
				datHeader.AddRange(BitConverter.GetBytes((compressedData.Count / 128) + 16));
				//Buffer Size
				datHeader.AddRange(BitConverter.GetBytes(compressedData.Count / 128));
				//Block Count
				datHeader.AddRange(BitConverter.GetBytes((short)5));
				//Unknown
				datHeader.AddRange(BitConverter.GetBytes((short)256));

				//Vertex Info Block Uncompressed
				var datPadding = 128 - (vertexInfoBlock.Count % 128);
				datPadding = (datPadding == 128 ? 0 : datPadding);
				datHeader.AddRange(BitConverter.GetBytes(vertexInfoBlock.Count + datPadding));
				//Model Data Block Uncompressed
				datPadding = 128 - (modelDataBlock.Count % 128);
				datPadding = (datPadding == 128 ? 0 : datPadding);
				datHeader.AddRange(BitConverter.GetBytes(modelDataBlock.Count + datPadding));
				//Vertex Data Block LoD[1] Uncompressed
				datPadding = 128 - (dbList[0].VertexDataBlock.Count % 128);
				datPadding = (datPadding == 128 ? 0 : datPadding);
				datHeader.AddRange(BitConverter.GetBytes(dbList[0].VertexDataBlock.Count + datPadding));
				//Vertex Data Block LoD[2] Uncompressed
				datPadding = 128 - (dbList[1].VertexDataBlock.Count % 128);
				datPadding = (datPadding == 128 ? 0 : datPadding);
				datHeader.AddRange(BitConverter.GetBytes(dbList[1].VertexDataBlock.Count + datPadding));
				//Vertex Data Block LoD[3] Uncompressed
				datPadding = 128 - (dbList[2].VertexDataBlock.Count % 128);
				datPadding = (datPadding == 128 ? 0 : datPadding);
				datHeader.AddRange(BitConverter.GetBytes(dbList[2].VertexDataBlock.Count + datPadding));
				//Blank 1
				datHeader.AddRange(BitConverter.GetBytes(0));
				//Blank 2
				datHeader.AddRange(BitConverter.GetBytes(0));
				//Blank 3
				datHeader.AddRange(BitConverter.GetBytes(0));
				//Index Data Block LoD[1] Uncompressed
				datPadding = 128 - (dbList[0].IndexDataBlock.Count % 128);
				datPadding = (datPadding == 128 ? 0 : datPadding);
				datHeader.AddRange(BitConverter.GetBytes(dbList[0].IndexDataBlock.Count + datPadding));
				//Index Data Block LoD[2] Uncompressed
				datPadding = 128 - (dbList[1].IndexDataBlock.Count % 128);
				datPadding = (datPadding == 128 ? 0 : datPadding);
				datHeader.AddRange(BitConverter.GetBytes(dbList[1].IndexDataBlock.Count + datPadding));
				//Index Data Block LoD[3] Uncompressed
				datPadding = 128 - (dbList[2].IndexDataBlock.Count % 128);
				datPadding = (datPadding == 128 ? 0 : datPadding);
				datHeader.AddRange(BitConverter.GetBytes(dbList[2].IndexDataBlock.Count + datPadding));

				//Vertex Info Block Compressed
				datHeader.AddRange(BitConverter.GetBytes(compVertexInfoSize));
				//Model Data Block Compressed
				datHeader.AddRange(BitConverter.GetBytes(compModelDataSize));
				//Vertex Data Block LoD[1] Compressed
				datHeader.AddRange(BitConverter.GetBytes(dbList[0].compVertexDataBlockSize));
				//Vertex Data Block LoD[2] Compressed
				datHeader.AddRange(BitConverter.GetBytes(dbList[1].compVertexDataBlockSize));
				//Vertex Data Block LoD[3] Compressed
				datHeader.AddRange(BitConverter.GetBytes(dbList[2].compVertexDataBlockSize));
				//Blank 1
				datHeader.AddRange(BitConverter.GetBytes(0));
				//Blank 2
				datHeader.AddRange(BitConverter.GetBytes(0));
				//Blank 3
				datHeader.AddRange(BitConverter.GetBytes(0));
				//Index Data Block LoD[1] Compressed
				datHeader.AddRange(BitConverter.GetBytes(dbList[0].compIndexDataBlockSize));
				//Index Data Block LoD[2] Compressed
				datHeader.AddRange(BitConverter.GetBytes(dbList[1].compIndexDataBlockSize));
				//Index Data Block LoD[3] Compressed
				datHeader.AddRange(BitConverter.GetBytes(dbList[2].compIndexDataBlockSize));

				var vertexInfoOffset = 0;
				var modelDataOffset = compVertexInfoSize;
				var vertexDataBlock1Offset = modelDataOffset + compModelDataSize;
				var indexDataBlock1Offset = vertexDataBlock1Offset + dbList[0].compVertexDataBlockSize;
				var vertexDataBlock2Offset = indexDataBlock1Offset + dbList[0].compIndexDataBlockSize;
				var indexDataBlock2Offset = vertexDataBlock2Offset + dbList[1].compVertexDataBlockSize;
				var vertexDataBlock3Offset = indexDataBlock2Offset + dbList[1].compIndexDataBlockSize;
				var indexDataBlock3Offset = vertexDataBlock3Offset + dbList[2].compVertexDataBlockSize;

				//Vertex Info Offset
				datHeader.AddRange(BitConverter.GetBytes(vertexInfoOffset));
				//Model Data Offset
				datHeader.AddRange(BitConverter.GetBytes(modelDataOffset));
				//Vertex Data Block LoD[1] Offset
				datHeader.AddRange(BitConverter.GetBytes(vertexDataBlock1Offset));
				//Vertex Data Block LoD[2] Offset
				datHeader.AddRange(BitConverter.GetBytes(vertexDataBlock2Offset));
				//Vertex Data Block LoD[3] Offset
				datHeader.AddRange(BitConverter.GetBytes(vertexDataBlock3Offset));
				//Blank 1
				datHeader.AddRange(BitConverter.GetBytes(0));
				//Blank 2
				datHeader.AddRange(BitConverter.GetBytes(0));
				//Blank 3
				datHeader.AddRange(BitConverter.GetBytes(0));
				//Index Data Block LoD[1] Offset
				datHeader.AddRange(BitConverter.GetBytes(indexDataBlock1Offset));
				//Index Data Block LoD[2] Offset
				datHeader.AddRange(BitConverter.GetBytes(indexDataBlock2Offset));
				//Index Data Block LoD[3] Offset
				datHeader.AddRange(BitConverter.GetBytes(indexDataBlock3Offset));


				var mdp = 1 + modelDataParts;
				var ind1 = mdp + dbList[0].VDBParts;
				var vert2 = ind1 + dbList[0].IDBParts;
				var ind2 = vert2 + dbList[1].VDBParts;
				var vert3 = ind2 + dbList[1].IDBParts;
				var ind3 = vert3 + dbList[2].VDBParts;

				//Vertex Info Index
				datHeader.AddRange(BitConverter.GetBytes((short)0));
				//Model Data Index
				datHeader.AddRange(BitConverter.GetBytes((short)1));
				//Vertex Data Block LoD[1] Index
				datHeader.AddRange(BitConverter.GetBytes((short)mdp));
				//Vertex Data Block LoD[2] Index
				datHeader.AddRange(BitConverter.GetBytes((short)vert2));
				//Vertex Data Block LoD[3] Index
				datHeader.AddRange(BitConverter.GetBytes((short)vert3));
				//Blank 1 (copies indices?)
				datHeader.AddRange(BitConverter.GetBytes((short)ind1));
				//Blank 2 (copies indices?)
				datHeader.AddRange(BitConverter.GetBytes((short)ind2));
				//Blank 3 (copies indices?)
				datHeader.AddRange(BitConverter.GetBytes((short)ind3));
				//Index Data Block LoD[1] Index
				datHeader.AddRange(BitConverter.GetBytes((short)ind1));
				//Index Data Block LoD[2] Index
				datHeader.AddRange(BitConverter.GetBytes((short)ind2));
				//Index Data Block LoD[3] Index
				datHeader.AddRange(BitConverter.GetBytes((short)ind3));


				//Vertex Info part count
				datHeader.AddRange(BitConverter.GetBytes((short)1));
				//Model Data part count
				datHeader.AddRange(BitConverter.GetBytes((short)modelDataParts));
				//Vertex Data Block LoD[1] part count
				datHeader.AddRange(BitConverter.GetBytes((short)dbList[0].VDBParts));
				//Vertex Data Block LoD[2] part count
				datHeader.AddRange(BitConverter.GetBytes((short)dbList[1].VDBParts));
				//Vertex Data Block LoD[3] part count
				datHeader.AddRange(BitConverter.GetBytes((short)dbList[2].VDBParts));
				//Blank 1 it s
				datHeader.AddRange(BitConverter.GetBytes((short)0));
				//Blank 2 
				datHeader.AddRange(BitConverter.GetBytes((short)0));
				//Blank 3 
				datHeader.AddRange(BitConverter.GetBytes((short)0));
				//Index Data Block LoD[1] part count
				datHeader.AddRange(BitConverter.GetBytes((short)dbList[0].IDBParts));
				//Index Data Block LoD[2] part count
				datHeader.AddRange(BitConverter.GetBytes((short)dbList[1].IDBParts));
				//Index Data Block LoD[3] part count
				datHeader.AddRange(BitConverter.GetBytes((short)dbList[2].IDBParts));

				//Mesh Count
				datHeader.AddRange(BitConverter.GetBytes((short)newMeshCount));
				//Material Count
				datHeader.AddRange(BitConverter.GetBytes((short)newMatStringCount));
				//Unknown 1
				datHeader.AddRange(BitConverter.GetBytes((short)259));
				//Unknown 2
				datHeader.AddRange(BitConverter.GetBytes((short)0));

				int VDBPartCount = 0;
				//Vertex Info padded size
				datHeader.AddRange(BitConverter.GetBytes((short)compVertexInfoSize));
				//Model Data padded size
				for(int i = 0; i < modelDataParts; i++)
				{
					datHeader.AddRange(BitConverter.GetBytes((short)compModelSizes[i]));
				}

				//Vertex Data Block LoD[0] part padded sizes
				for (int i = 0; i < dbList[0].VDBParts; i++)
				{
					datHeader.AddRange(BitConverter.GetBytes((short)compMeshSizes[i]));
				}

				VDBPartCount += dbList[0].VDBParts;
				//Index Data Block LoD[0] padded size
				for(int i = 0; i < dbList[0].IDBParts; i++)
				{
					datHeader.AddRange(BitConverter.GetBytes((short)compIndexSizes[i]));
				}

				//Vertex Data Block LoD[1] part padded sizes
				for (int i = 0; i < dbList[1].VDBParts; i++)
				{
					datHeader.AddRange(BitConverter.GetBytes((short)compMeshSizes[VDBPartCount + i]));
				}
				VDBPartCount += dbList[1].VDBParts;

                //Index Data Block LoD[1] padded size
                for (int i = 0; i < dbList[1].IDBParts; i++)
                {
                    datHeader.AddRange(BitConverter.GetBytes((short)compIndexSizes[i]));
                }
                
				//Vertex Data Block LoD[2] part padded sizes
				for (int i = 0; i < dbList[2].VDBParts; i++)
				{
					datHeader.AddRange(BitConverter.GetBytes((short)compMeshSizes[VDBPartCount + i]));
				}

                //Index Data Block LoD[2] padded size
                for (int i = 0; i < dbList[2].IDBParts; i++)
                {
                    datHeader.AddRange(BitConverter.GetBytes((short)compIndexSizes[i]));
                }

                //Rest of header
                if(datHeader.Count > headerLength)
                {
                    FlexibleMessageBox.Show("Model Size/Header exceeded allowed size/length.\n\n" +
                         "Please reduce model complexity and try again.\n\nThe import has been cancelled.", "ImportModel Error " + Info.appVersion, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                if (datHeader.Count != headerLength)
				{
					var headerEnd = headerLength - (datHeader.Count % headerLength);
					datHeader.AddRange(new byte[headerEnd]);
				}
			}
			compressedData.InsertRange(0, datHeader);
            #endregion

            WriteToDat(compressedData, modEntry, inModList, internalPath, category, itemName, lineNum);
		}

		public class DataBlocks
		{
			public int compVertexDataBlockSize = 0;
			public int compIndexDataBlockSize = 0;
			public int VDBParts = 0;
			public int IDBParts = 0;

			public List<byte> VertexDataBlock = new List<byte>();
			public List<byte> IndexDataBlock = new List<byte>();
		}

		public class ImportData
		{
			public List<byte> dataSet1 = new List<byte>();
			public List<byte> dataSet2 = new List<byte>();
			public List<byte> indexSet = new List<byte>();
		}

		public class ColladaData
		{
			public string[] bones;
            public int indexStride;

			public List<float> vertex = new List<float>();
			public List<float> normal = new List<float>();
			public List<float> texCoord = new List<float>();
			public List<float> texCoord2 = new List<float>();
			public List<float> weights = new List<float>();
			public List<float> biNormal = new List<float>();
			public List<float> tangent = new List<float>();
            public List<float> vertexColors = new List<float>();
            public List<float> vertexAlphas = new List<float>();
            public List<int> index = new List<int>();
			public List<int> bIndex = new List<int>();
			public List<int> vCount = new List<int>();

			public List<int> vIndexList = new List<int>();
			public List<int> nIndexList = new List<int>();
			public List<int> bnIndexList = new List<int>();
			public List<int> tcIndexList = new List<int>();
			public List<int> tc2IndexList = new List<int>();
            public List<int> vcIndexList = new List<int>();
            public List<int> vaIndexList = new List<int>();

            public Dictionary<int, int> partsDict = new Dictionary<int, int>();
		}

		public class ColladaMeshData
		{
			public MeshGeometry3D meshGeometry = new MeshGeometry3D();

			public List<byte[]> blendIndices = new List<byte[]>();
			public List<byte[]> blendWeights = new List<byte[]>();
			public List<int> handedness = new List<int>();
			public Vector2Collection texCoord2 = new Vector2Collection();
            public Vector3Collection vertexColors = new Vector3Collection();
            public List<float> vertexAlphas = new List<float>();

            public Dictionary<int, int> partsDict = new Dictionary<int, int>();
		}


		/// <summary>
		/// Writes the newly imported data to the .dat for modifications.
		/// </summary>
		/// <param name="data">The data to be written.</param>
		/// <param name="modEntry">The modlist entry (if any) for the given file.</param>
		/// <param name="inModList">Is the item already contained within the mod list.</param>
		/// <param name="internalFilePath">The internal file path of the item being modified.</param>
		/// <param name="category">The category of the item.</param>
		/// <param name="itemName">The name of the item being modified.</param>
		/// <param name="lineNum">The line number of the existing mod list entry for the item if it exists.</param>
		/// <returns>The new offset in which the modified data was placed.</returns>
		public static int WriteToDat(List<byte> data, JsonEntry modEntry, bool inModList, string internalFilePath, string category, string itemName, int lineNum)
		{
			int offset = 0;
			bool dataOverwritten = false;

			if (inModList)
			{
				if (modEntry.modOffset == 0)
				{
					FlexibleMessageBox.Show("TexTools detected a Mod List entry with a Mod Offset of 0.\n\n" +
						"Please submit a bug report along with your modlist file.", "ImportModel Error " + Info.appVersion, MessageBoxButtons.OK, MessageBoxIcon.Error);
					return 0;
				}
				else if (modEntry.originalOffset == 0)
				{
				   FlexibleMessageBox.Show("TexTools detected a Mod List entry with an Original Offset of 0.\n\n" +
						"Please submit a bug report along with your modlist file.", "ImportModel Error " + Info.appVersion, MessageBoxButtons.OK, MessageBoxIcon.Error);
					return 0;
				}
			}

		    var datNum = int.Parse(Info.ModDatDict[Strings.ItemsDat]);
		    var modDatPath = string.Format(Info.datDir, Strings.ItemsDat, datNum);

            if (inModList)
		    {
		        datNum = ((modEntry.modOffset / 8) & 0x0F) / 2;
		        modDatPath = string.Format(Info.datDir, modEntry.datFile, datNum);
		    }
		    else
		    {
		        var fileLength = new FileInfo(modDatPath).Length;
		        while (fileLength + data.Count >= 2000000000)
		        {
		            datNum += 1;
		            modDatPath = string.Format(Info.datDir, Strings.ItemsDat, datNum);
		            if (!File.Exists(modDatPath))
		            {
		                CreateDat.MakeNewDat(Strings.ItemsDat);
		            }
		            fileLength = new FileInfo(modDatPath).Length;
		        }
            }

            try
			{
                /* 
                * If the item has been previously modified and the compressed data being imported is smaller or equal to the exisiting data
                * replace the existing data with new data.
                */
                if (inModList && data.Count <= modEntry.modSize)
                {
                    if (modEntry.modOffset != 0)
                    {
                        datNum = ((modEntry.modOffset / 8) & 0x0F) / 2;
                        modDatPath = string.Format(Info.datDir, modEntry.datFile, datNum);
                        var datOffsetAmount = datNum * 16;

                        using (BinaryWriter bw = new BinaryWriter(File.OpenWrite(modDatPath)))
                        {
                            int sizeDiff = modEntry.modSize - data.Count;

                            bw.BaseStream.Seek(modEntry.modOffset - datOffsetAmount, SeekOrigin.Begin);

                            bw.Write(data.ToArray());

                            bw.Write(new byte[sizeDiff]);

                            Helper.UpdateIndex(modEntry.modOffset, internalFilePath, Strings.ItemsDat);
                            Helper.UpdateIndex2(modEntry.modOffset, internalFilePath, Strings.ItemsDat);

                            offset = modEntry.modOffset;

                            dataOverwritten = true;
                        }
                    }
                }
                else
                {
                    int emptyLength = 0;
                    int emptyLine = 0;

                    /* 
                     * If there is an empty entry in the modlist and the compressed data being imported is smaller or equal to the available space
                    *  write the compressed data in the existing space.
                    */

                    try
                    {
                        foreach (string line in File.ReadAllLines(Properties.Settings.Default.Modlist_Directory))
                        {
                            JsonEntry emptyEntry = JsonConvert.DeserializeObject<JsonEntry>(line);

                            if (emptyEntry.fullPath.Equals("") && emptyEntry.datFile.Equals(Strings.ItemsDat))
                            {
                                if (emptyEntry.modOffset != 0)
                                {
                                    emptyLength = emptyEntry.modSize;

                                    if (emptyLength > data.Count)
                                    {
                                        int sizeDiff = emptyLength - data.Count;

                                        datNum = ((emptyEntry.modOffset / 8) & 0x0F) / 2;
                                        modDatPath = string.Format(Info.datDir, emptyEntry.datFile, datNum);
                                        var datOffsetAmount = datNum * 16;

                                        using (BinaryWriter bw = new BinaryWriter(File.OpenWrite(modDatPath)))
                                        {
                                            bw.BaseStream.Seek(emptyEntry.modOffset - datOffsetAmount, SeekOrigin.Begin);

                                            bw.Write(data.ToArray());

                                            bw.Write(new byte[sizeDiff]);
                                        }

                                        int originalOffset = Helper.UpdateIndex(emptyEntry.modOffset, internalFilePath, Strings.ItemsDat) * 8;
                                        Helper.UpdateIndex2(emptyEntry.modOffset, internalFilePath, Strings.ItemsDat);

                                        if (inModList)
                                        {
                                            originalOffset = modEntry.originalOffset;

                                            JsonEntry replaceOriginalEntry = new JsonEntry()
                                            {
                                                category = String.Empty,
                                                name = String.Empty,
                                                fullPath = String.Empty,
                                                originalOffset = 0,
                                                modOffset = modEntry.modOffset,
                                                modSize = modEntry.modSize,
                                                datFile = Strings.ItemsDat
                                            };

                                            string[] oLines = File.ReadAllLines(Properties.Settings.Default.Modlist_Directory);
                                            oLines[lineNum] = JsonConvert.SerializeObject(replaceOriginalEntry);
                                            File.WriteAllLines(Properties.Settings.Default.Modlist_Directory, oLines);
                                        }

                                        JsonEntry replaceEntry = new JsonEntry()
                                        {
                                            category = category,
                                            name = itemName,
                                            fullPath = internalFilePath,
                                            originalOffset = originalOffset,
                                            modOffset = emptyEntry.modOffset,
                                            modSize = emptyEntry.modSize,
                                            datFile = Strings.ItemsDat
                                        };

                                        string[] lines = File.ReadAllLines(Properties.Settings.Default.Modlist_Directory);
                                        lines[emptyLine] = JsonConvert.SerializeObject(replaceEntry);
                                        File.WriteAllLines(Properties.Settings.Default.Modlist_Directory, lines);

                                        offset = emptyEntry.modOffset;

                                        dataOverwritten = true;
                                        break;
                                    }
                                }
                            }
                            emptyLine++;
                        }
                    }
                    catch (Exception ex)
                    {
                        FlexibleMessageBox.Show("Error Accessing .modlist File \n" + ex.Message, "ImportModel Error " + Info.appVersion, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }


                    if (!dataOverwritten)
                    {
                        using (BinaryWriter bw = new BinaryWriter(File.OpenWrite(modDatPath)))
                        {
                            bw.BaseStream.Seek(0, SeekOrigin.End);

                            while ((bw.BaseStream.Position & 0xFF) != 0)
                            {
                                bw.Write((byte)0);
                            }

                            int eof = (int)bw.BaseStream.Position + data.Count;

                            while ((eof & 0xFF) != 0)
                            {
                                data.AddRange(new byte[16]);
                                eof = eof + 16;
                            }

                            var datOffsetAmount = datNum * 16;
                            offset = (int)bw.BaseStream.Position + datOffsetAmount;

                            if (offset != 0)
                            {
                                bw.Write(data.ToArray());
                            }
                            else
                            {
                                FlexibleMessageBox.Show("There was an issue obtaining the .dat4 offset to write data to, try importing again. " +
                                                        "\n\n If the problem persists, please submit a bug report.", "ImportModel Error " + Info.appVersion, MessageBoxButtons.OK, MessageBoxIcon.Error);
                                return 0;
                            }
                        }
                    }
                }
            }
			catch (Exception ex)
			{
				FlexibleMessageBox.Show("Error Accessing .dat4 File \n" + ex.Message, "ImportModel Error " + Info.appVersion, MessageBoxButtons.OK, MessageBoxIcon.Error);
				return 0;
			}


			if (!dataOverwritten)
			{
				if(offset != 0)
				{
					int oldOffset = Helper.UpdateIndex(offset, internalFilePath, Strings.ItemsDat) * 8;
					Helper.UpdateIndex2(offset, internalFilePath, Strings.ItemsDat);

					/*
					 * If the item has been previously modifed, but the new compressed data to be imported is larger than the existing data
					 * remove the data from the modlist, leaving the offset and size intact for future use
					*/
					if (modEntry != null && inModList && data.Count > modEntry.modSize)
					{
						oldOffset = modEntry.originalOffset;

						JsonEntry replaceEntry = new JsonEntry()
						{
							category = String.Empty,
							name = String.Empty,
							fullPath = String.Empty,
							originalOffset = 0,
							modOffset = modEntry.modOffset,
							modSize = modEntry.modSize,
							datFile = Strings.ItemsDat
						};

						string[] lines = File.ReadAllLines(Properties.Settings.Default.Modlist_Directory);
						lines[lineNum] = JsonConvert.SerializeObject(replaceEntry);
						File.WriteAllLines(Properties.Settings.Default.Modlist_Directory, lines);
					}

					JsonEntry entry = new JsonEntry()
					{
						category = category,
						name = itemName,
						fullPath = internalFilePath,
						originalOffset = oldOffset,
						modOffset = offset,
						modSize = data.Count,
						datFile = Strings.ItemsDat
					};

					try
					{
						using (StreamWriter modFile = new StreamWriter(Properties.Settings.Default.Modlist_Directory, true))
						{
							modFile.BaseStream.Seek(0, SeekOrigin.End);
							modFile.WriteLine(JsonConvert.SerializeObject(entry));
						}
					}
					catch (Exception ex)
					{
						FlexibleMessageBox.Show("Error Accessing .modlist File \n" + ex.Message, "ImportModel Error " + Info.appVersion, MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
			}

			return offset;
		}

		/// <summary>
		/// Compresses raw byte data.
		/// </summary>
		/// <param name="uncomp">The data to be compressed.</param>
		/// <returns>The compressed byte data.</returns>
		private static byte[] Compressor(byte[] uncomp)
		{
			using (MemoryStream uncompressedMS = new MemoryStream(uncomp))
			{
				byte[] compbytes = null;
				using (var compressedMS = new MemoryStream())
				{
					using (var ds = new DeflateStream(compressedMS, CompressionMode.Compress))
					{
						uncompressedMS.CopyTo(ds);
						ds.Close();
						compbytes = compressedMS.ToArray();
					}
				}
				return compbytes;
			}
		}
	}
}
