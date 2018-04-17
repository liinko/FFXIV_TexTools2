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


		public static void ImportDAE(string category, string itemName, string modelName, string selectedMesh, string internalPath, List<string> boneStrings, ModelData modelData, Dictionary<string, ImportSettings> settings)
		{
			importSettings = settings;

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
			}

			var numMeshes = modelData.LoD[0].MeshCount;

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
				string pos = "-positions-array";
				string norm = "-normals-array";
				string biNorm = "-texbinormals";
				string tang = "-textangents";
				int tcStride = 2;
				int indStride = 4;
				bool blender = false;

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

				Dictionary<string, int> boneDict = new Dictionary<string, int>();
				Dictionary<string, string> meshNameDict = new Dictionary<string, string>();
				List<string> extraBones = new List<string>();

				for (int i = 0; i < boneStrings.Count; i++)
				{
					boneDict.Add(boneStrings[i], i);
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
										texc = "-map1-array";
										texc2 = "-map2-array";
										biNorm = "-map1-texbinormals";
										tang = "-map1-textangents";
										tcStride = 3;
										indStride = 6;
									}
									else if (tool.Contains("FBX"))
									{
										pos = "-position-array";
										norm = "-normal0-array";
										texc = "-uv0-array";
										texc2 = "-uv1-array";
									}
									else if (tool.Contains("Exporter for Blender"))
									{
										biNorm = "-bitangents-array";
										tang = "-tangents-array";
										texc = "-texcoord-0-array";
										texc2 = "-texcoord-1-array";
										indStride = 1;
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

                                    var meshNum = int.Parse(atr.Substring(atr.LastIndexOf("_") + 1, 1));

									if (atr.Contains("."))
									{
										meshNum = int.Parse(atr.Substring(atr.LastIndexOf("_") + 1, atr.LastIndexOf(".") - (atr.LastIndexOf("_") + 1)));
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
											}
											//Indices
											if (reader.Name.Equals("p"))
											{
												cData.index.AddRange((int[])reader.ReadElementContentAs(typeof(int[]), null));

												if (cData.texCoord2.Count < 1 && indStride == 6)
												{
													indStride = 4;
												}

												for (int i = 0; i < cData.index.Count; i += indStride)
												{
													cData.vIndexList.Add(cData.index[i]);
													cData.nIndexList.Add(cData.index[i + 1]);
													cData.tcIndexList.Add(cData.index[i + 2]);

													if (cData.texCoord2.Count > 0 && indStride == 6)
													{
														cData.tc2IndexList.Add(cData.index[i + 4]);
													}
													else if (cData.texCoord2.Count > 0 && indStride == 4)
													{
														cData.tc2IndexList.Add(cData.index[i + 2]);
													}

													if (cData.biNormal.Count > 0)
													{
														cData.bnIndexList.Add(cData.index[i + 3]);
													}

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
									        pDict[meshNum].Add(int.Parse(num), cData);
									    }
									    catch (Exception e)
									    {
									        FlexibleMessageBox.Show("Duplicate mesh part found.\n" +
									                                "Mesh: " + meshNum + "\tPart: " + num + "\n" +
                                                                    "Full Name: " + atr + "\n\n" +
									                                "Delete or Rename the duplicate mesh part and try again.\n\n" +
									                                "Error: " + e.Message, "ImportModel Error " + Info.appVersion, MessageBoxButtons.OK, MessageBoxIcon.Error);
                                        }
									}
									else
									{
									    try
									    {
									        pDict[meshNum].Add(0, cData);

									    }
									    catch (Exception e)
									    {
									        FlexibleMessageBox.Show("Duplicate mesh part found.\n" +
									                                "Mesh: " + meshNum + "\tPart: 0" + "\n" +
									                                "Full Name: " + atr + "\n\n" +
                                                                    "Delete or Rename the duplicate mesh part and try again.\n\n" +
									                                "Error: " + e.Message, "ImportModel Error " + Info.appVersion, MessageBoxButtons.OK, MessageBoxIcon.Error);
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

													if (!boneDict.ContainsKey(bString))
													{
														if (!extraBones.Contains(bString))
														{
															extraBones.Add(bString);
														}
													}
													else
													{
													    try
													    {
													        cData.bIndex.Add(boneDict[bString]);
													    }
													    catch (Exception e)
													    {
													        FlexibleMessageBox.Show("Error reading bone data.\n\nBone: " + bString +
													                                "\n\n" + e.Message, "ImportModel Error " + Info.appVersion, MessageBoxButtons.OK, MessageBoxIcon.Error);
													        return;
                                                        }

                                                        cData.bIndex.Add(tempbIndex[a + 1]);
													}
												}

												if(extraBones.Count > 0)
												{
													throw new KeyNotFoundException();
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
					Debug.WriteLine(e.StackTrace);
					if(extraBones.Count > 0)
					{
						string bones = "";
						foreach(var b in extraBones)
						{
							bones += "* " + b + "\n";
						}

					   FlexibleMessageBox.Show("TexTools detected bones that are not part of the original model.\n" +
							"Importing extra bones is not supported, delete the extra bones and try importing again.\n\nExtra Bone(s):\n" + bones, "ImportModel Error " + Info.appVersion, MessageBoxButtons.OK, MessageBoxIcon.Error);

					}
					else
					{
						FlexibleMessageBox.Show("Error reading .dae file.\n" + e.Message, "ImportModel Error " + Info.appVersion, MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
					return;
				}

				for(int i = 0; i < pDict.Count; i++)
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

						        if (mDict[c].texCoord2.Count < 1 && indStride == 6)
						        {
						            indStride = 4;
						        }

						        cdDict[i].vertex.AddRange(mDict[c].vertex);
						        cdDict[i].normal.AddRange(mDict[c].normal);
						        cdDict[i].texCoord.AddRange(mDict[c].texCoord);
						        cdDict[i].texCoord2.AddRange(mDict[c].texCoord2);
						        cdDict[i].tangent.AddRange(mDict[c].tangent);
						        cdDict[i].biNormal.AddRange(mDict[c].biNormal);

						        for (int k = 0; k < mDict[c].vIndexList.Count; k++)
						        {
						            cdDict[i].index.Add(mDict[c].vIndexList[k] + vMax);
						            cdDict[i].index.Add(mDict[c].nIndexList[k] + nMax);
						            cdDict[i].index.Add(mDict[c].tcIndexList[k] + tcMax);

						            if (mDict[c].texCoord2.Count > 0)
						            {
						                cdDict[i].index.Add(mDict[c].tc2IndexList[k] + tc2Max);
						            }

						            if (mDict[c].biNormal.Count > 0)
						            {
						                cdDict[i].index.Add(mDict[c].bnIndexList[k] + bnMax);
						            }
						        }

						        vMax += mDict[c].vIndexList.Max() + 1;
						        nMax += mDict[c].nIndexList.Max() + 1;
						        tcMax += mDict[c].tcIndexList.Max() + 1;

						        if (mDict[c].texCoord2.Count > 0)
						        {
						            tc2Max += mDict[c].tc2IndexList.Max() + 1;
						        }

						        if (mDict[c].biNormal.Count > 0)
						        {
						            bnMax += mDict[c].bnIndexList.Max() + 1;
						        }

						        cdDict[i].partsDict.Add(c, mDict[c].index.Count / indStride);

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
				foreach (var cd in cdDict.Values)
				{
					ColladaMeshData cmd = new ColladaMeshData();

					Vector3Collection Vertex = new Vector3Collection();
					Vector2Collection TexCoord = new Vector2Collection();
					Vector2Collection TexCoord2 = new Vector2Collection();
					Vector3Collection Normals = new Vector3Collection();
					Vector3Collection Tangents = new Vector3Collection();
					Vector3Collection BiNormals = new Vector3Collection();
					IntCollection Indices = new IntCollection();
					List<byte[]> blendIndices = new List<byte[]>();
					List<byte[]> blendWeights = new List<byte[]>();
					List<string> boneStringList = new List<string>();


					Vector3Collection nVertex = new Vector3Collection();
					Vector2Collection nTexCoord = new Vector2Collection();
					Vector2Collection nTexCoord2 = new Vector2Collection();
					Vector3Collection nNormals = new Vector3Collection();
					Vector3Collection nTangents = new Vector3Collection();
					Vector3Collection nBiNormals = new Vector3Collection();
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

					if(cd.biNormal.Count > 0)
					{
						for (int i = 0; i < cd.biNormal.Count; i += 3)
						{
							BiNormals.Add(new SharpDX.Vector3(cd.biNormal[i], cd.biNormal[i + 1], cd.biNormal[i + 2]));
						}
					}

					if (cd.tangent.Count > 0)
					{
						for (int i = 0; i < cd.tangent.Count; i += 3)
						{
							Tangents.Add(new SharpDX.Vector3(cd.tangent[i], cd.tangent[i + 1], cd.tangent[i + 2]));
						}
					}

					for (int i = 0; i < cd.texCoord.Count; i += tcStride)
					{
						TexCoord.Add(new SharpDX.Vector2(cd.texCoord[i], cd.texCoord[i + 1]));
					}

					for (int i = 0; i < cd.texCoord2.Count; i += tcStride)
					{
						TexCoord2.Add(new SharpDX.Vector2(cd.texCoord2[i], cd.texCoord2[i + 1]));
					}

					Dictionary<int, int> errorDict = new Dictionary<int, int>();

					int vTrack = 0;

					Dictionary<int, int> blDict = new Dictionary<int, int>();
					var bli = modelData.LoD[0].MeshList[m].MeshInfo.BoneListIndex;
					var bld = modelData.BoneSet[m];


					for (int i = 0; i < bld.BoneCount; i++)
					{
						blDict.Add(bld.BoneData[i], i);
					}

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
								var bi = (byte)blDict[b];
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
								errorDict.Add(i, diff);
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
					   FlexibleMessageBox.Show("TexTools detected that mesh " + m + " refrences bones that were not originally part of that mesh.\n\n" +
							"This may cause things to not look correct in-game, but will most likely be fine.\n\n" +
							"TexTools will now continue importing.", "Mesh Import Warning",MessageBoxButtons.OK,MessageBoxIcon.Warning);

						vTrack = 0;
						blDict.Clear();
						errorDict.Clear();
						blendIndices.Clear();
						blendWeights.Clear();

						//use the boneset for mesh 0 
						bld = modelData.BoneSet[0];

						for (int i = 0; i < bld.BoneCount; i++)
						{
							blDict.Add(bld.BoneData[i], i);
						}

						for (int i = 0; i < Vertex.Count; i++)
						{
						    try
						    {
						        int bCount = cd.vCount[i];

						        int boneSum = 0;

						        List<byte> biList = new List<byte>();
						        List<byte> bwList = new List<byte>();

						        int newbCount = 0;
						        for (int j = 0; j < bCount * 2; j += 2)
						        {
						            var b = cd.bIndex[vTrack * 2 + j];
                                    try
						            {
						                var bi = (byte) blDict[b];
						                var bw = (byte) Math.Round(cd.weights[cd.bIndex[vTrack * 2 + j + 1]] * 255f);

						                if (bw != 0)
						                {
						                    biList.Add(bi);
						                    bwList.Add(bw);
						                    boneSum += bw;
						                    newbCount++;
						                }
						            }
						            catch (Exception ex)
						            {
						                FlexibleMessageBox.Show("There was an error reading imported bone data at:" +
						                                        "\n\nVertex: " + i + " Bone: " + b + "\n\n" + ex.Message, "Bone Read Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                        return;
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
						            errorDict.Add(i, diff);
						            if (diff < 0)
						            {
						                bwList[maxIndex] += (byte) Math.Abs(diff);
						            }
						            else
						            {
						                // Subtract difference when over-weight.
						                bwList[maxIndex] -= (byte) Math.Abs(diff);
						            }
						        }

						        boneSum = 0;
						        bwList.ForEach(x => boneSum += x);

						        blendIndices.Add(biList.ToArray());
						        blendWeights.Add(bwList.ToArray());
						        vTrack += originalbCount;
						    }
						    catch (Exception e)
						    {
						        FlexibleMessageBox.Show("There was an error reading imported bone data at:" +
						                                "\n\nVertex: " + i + "\n\n" + e.Message, "Bone Read Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                return;
						    }

						}
					}

					if (errorDict.Count > 0)
					{
						string errorString = "";
						foreach(var er in errorDict)
						{
							errorString += "Vertex: " + er.Key + "\t Correction Amount: " + er.Value + "\n";
						}


						FlexibleMessageBox.Show("Corrected bone weights on the following vertices :\n\n" + errorString, "Weight Correction", MessageBoxButtons.OK, MessageBoxIcon.Information);
						//FlexibleMessageBox.Show("TexTools detected an affected bone count greater than 4.\n\n" +
						//	"TexTools removed the smallest weight counts from the following: \n\n" + errorString, "Over Weight Count",MessageBoxButtons.OK,MessageBoxIcon.Information);
					}


					var extraVertDict = modelData.LoD[0].MeshList[m].extraVertDict;

					Dictionary<int, int> indexDict = new Dictionary<int, int>();
					var inCount = 0;

					List<int> handedness = new List<int>();


					List<int[]> iList = new List<int[]>();

					var stride = 5;
					var indexMax = 0;

					if (TexCoord2.Count < 1)
					{
						stride = 4;
					}

					for(int i = 0; i < cd.index.Count; i += stride)
					{
						iList.Add(cd.index.GetRange(i, stride).ToArray());
					}

					if(cd.index.Count > 0)
					{
						indexMax = cd.index.Max();
					}

					for (int i = 0; i < iList.Count; i++)
					{
					    try
					    {
					        if (!indexDict.ContainsKey(iList[i][0]))
					        {
					            if (!blender)
					            {
					                indexDict.Add(iList[i][0], inCount);
					                nVertex.Add(Vertex[iList[i][0]]);
					                nBlendIndices.Add(blendIndices[iList[i][0]]);
					                nBlendWeights.Add(blendWeights[iList[i][0]]);
					                nNormals.Add(Normals[iList[i][1]]);
					                nTexCoord.Add(TexCoord[iList[i][2]]);
					                if (TexCoord2.Count > 0 && TexCoord2.Count >= indexMax)
					                {
					                    nTexCoord2.Add(TexCoord2[iList[i][3]]);
					                }

					                if (nTangents.Count > 0 && TexCoord2.Count > 0)
					                {
					                    nTangents.Add(Tangents[iList[i][4]]);
					                    nBiNormals.Add(BiNormals[iList[i][4]]);
					                }
					                else if (nTangents.Count > 0 && TexCoord2.Count < 1)
					                {
					                    nTangents.Add(Tangents[iList[i][3]]);
					                    nBiNormals.Add(BiNormals[iList[i][3]]);
					                }
					            }
					            else
					            {
					                indexDict.Add(iList[i][0], inCount);
					                nVertex.Add(Vertex[iList[i][0]]);
					                nBlendIndices.Add(blendIndices[iList[i][0]]);
					                nBlendWeights.Add(blendWeights[iList[i][0]]);
					                nNormals.Add(Normals[iList[i][0]]);
					                nTexCoord.Add(TexCoord[iList[i][0]]);
					                nTexCoord2.Add(TexCoord2[iList[i][0]]);

					                if (nTangents.Count > 0)
					                {
					                    nTangents.Add(Tangents[iList[i][0]]);
					                    nBiNormals.Add(BiNormals[iList[i][0]]);
					                }
					            }


					            inCount++;
					        }
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

					for (int i = 0; i < iList.Count; i++)
					{
					    try
					    {
					        var nIndex = indexDict[iList[i][0]];
					        Indices.Add(nIndex);
					    }
					    catch (Exception e)
					    {
					        FlexibleMessageBox.Show("There was an error finding the index data for:" +
					                                "\n\nIndex: " + i + "\n\n" + e.Message, "Index Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
					        return;
                        }
					}

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

					//if (cd.biNormal.Count > 0)
					//{
					//    mg.BiTangents = nBiNormals;
					//}

					//if (cd.tangent.Count > 0)
					//{
					//    mg.Tangents = Tangents;
					//}

					SharpDX.Vector3[] tan1 = new SharpDX.Vector3[Vertex.Count];
					SharpDX.Vector3[] tan2 = new SharpDX.Vector3[Vertex.Count];
					for (int a = 0; a < Indices.Count; a += 3)
					{
						int i1 = Indices[a];
						int i2 = Indices[a + 1];
						int i3 = Indices[a + 2];
						SharpDX.Vector3 v1 = nVertex[i1];
						SharpDX.Vector3 v2 = nVertex[i2];
						SharpDX.Vector3 v3 = nVertex[i3];
						SharpDX.Vector2 w1 = nTexCoord[i1];
						SharpDX.Vector2 w2 = nTexCoord[i2];
						SharpDX.Vector2 w3 = nTexCoord[i3];
						float x1 = v2.X - v1.X;
						float x2 = v3.X - v1.X;
						float y1 = v2.Y - v1.Y;
						float y2 = v3.Y - v1.Y;
						float z1 = v2.Z - v1.Z;
						float z2 = v3.Z - v1.Z;
						float s1 = w2.X - w1.X;
						float s2 = w3.X - w1.X;
						float t1 = w2.Y - w1.Y;
						float t2 = w3.Y - w1.Y;
						float r = 1.0f / (s1 * t2 - s2 * t1);
						SharpDX.Vector3 sdir = new SharpDX.Vector3((t2 * x1 - t1 * x2) * r, (t2 * y1 - t1 * y2) * r, (t2 * z1 - t1 * z2) * r);
						SharpDX.Vector3 tdir = new SharpDX.Vector3((s1 * x2 - s2 * x1) * r, (s1 * y2 - s2 * y1) * r, (s1 * z2 - s2 * z1) * r);
						tan1[i1] += sdir;
						tan1[i2] += sdir;
						tan1[i3] += sdir;
						tan2[i1] += tdir;
						tan2[i2] += tdir;
						tan2[i3] += tdir;
					}

					float d;
					SharpDX.Vector3 tmpt;
					for (int a = 0; a < nVertex.Count; ++a)
					{
						SharpDX.Vector3 n = SharpDX.Vector3.Normalize(nNormals[a]);
						SharpDX.Vector3 t = SharpDX.Vector3.Normalize(tan1[a]);
						d = (SharpDX.Vector3.Dot(SharpDX.Vector3.Cross(n, t), tan2[a]) < 0.0f) ? -1.0f : 1.0f;
						tmpt = new SharpDX.Vector3(t.X, t.Y, t.Z);
						mg.BiTangents.Add(tmpt);
						cmd.handedness.Add((int)d);
					}
					cmd.meshGeometry = mg;
					cmd.blendIndices = nBlendIndices;
					cmd.blendWeights = nBlendWeights;
					cmd.partsDict = cd.partsDict;
					cmd.texCoord2 = nTexCoord2;

					cmdList.Add(cmd);

					m++;
				}

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
					id.dataSet2.AddRange(BitConverter.GetBytes(4294967295));

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
					id.indexSet.AddRange(BitConverter.GetBytes((short)i));
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

				int vertexInfoSize = (MDLDatData.Item2) * 136;
				vertexInfoBlock.AddRange(br.ReadBytes(vertexInfoSize));


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
				modelDataBlock.AddRange(br.ReadBytes(4));

				//string block size (int)
				int stringBlockSize = br.ReadInt32();
				modelDataBlock.AddRange(BitConverter.GetBytes(stringBlockSize));

				//string block
				var stringBlock = br.ReadBytes(stringBlockSize);

				modelDataBlock.AddRange(stringBlock);

				//unknown (int)
				modelDataBlock.AddRange(br.ReadBytes(4));

				//mesh count (short)
				short meshCount = br.ReadInt16();
				modelDataBlock.AddRange(BitConverter.GetBytes(meshCount));

				//num of atr strings (short)
				short atrStringCount = br.ReadInt16();
				modelDataBlock.AddRange(BitConverter.GetBytes(atrStringCount));

				//num of mesh parts (short)
				short meshPartCount = br.ReadInt16();
				modelDataBlock.AddRange(BitConverter.GetBytes(meshPartCount));

				//num of material strings (short)
				short matStringCount = br.ReadInt16();
				modelDataBlock.AddRange(BitConverter.GetBytes(matStringCount));

				//num of bone strings (short)
				short boneStringCount = br.ReadInt16();
				modelDataBlock.AddRange(BitConverter.GetBytes(boneStringCount));

				//bone list count (short)
				short boneListCount = br.ReadInt16();
				modelDataBlock.AddRange(BitConverter.GetBytes(boneListCount));

				short unk1 = br.ReadInt16();
				modelDataBlock.AddRange(BitConverter.GetBytes(unk1));

				short unk2 = br.ReadInt16();
				modelDataBlock.AddRange(BitConverter.GetBytes(unk2));

				short unk3 = br.ReadInt16();
				modelDataBlock.AddRange(BitConverter.GetBytes(unk3));

				short unk4 = br.ReadInt16();
				modelDataBlock.AddRange(BitConverter.GetBytes(unk4));

				short unk5 = br.ReadInt16();
				modelDataBlock.AddRange(BitConverter.GetBytes(unk5));

				short unk6 = br.ReadInt16();
				modelDataBlock.AddRange(BitConverter.GetBytes(unk6));

				short unk7 = br.ReadInt16();
				modelDataBlock.AddRange(BitConverter.GetBytes(unk7));

				short unk8 = br.ReadInt16();
				modelDataBlock.AddRange(BitConverter.GetBytes(unk8));

				short unk9 = br.ReadInt16();
				modelDataBlock.AddRange(BitConverter.GetBytes(unk9));

				short unk10 = br.ReadInt16();
				modelDataBlock.AddRange(BitConverter.GetBytes(unk10));

				short unk11 = br.ReadInt16();
				modelDataBlock.AddRange(BitConverter.GetBytes(unk11));

				short unk12 = br.ReadInt16();
				modelDataBlock.AddRange(BitConverter.GetBytes(unk12));

				List<string> materials = new List<string>();
				using (BinaryReader br1 = new BinaryReader(new MemoryStream(stringBlock)))
				{
					br1.BaseStream.Seek(0, SeekOrigin.Begin);

					for (int i = 0; i < atrStringCount; i++)
					{
						while (br1.ReadByte() != 0)
						{
							//extract each atribute string here
						}
					}

					for (int i = 0; i < boneStringCount; i++)
					{

					}

					for (int i = 0; i < matStringCount; i++)
					{
						byte b;
						List<byte> name = new List<byte>();
						while ((b = br1.ReadByte()) != 0)
						{
							name.Add(b);
						}
						string material = Encoding.ASCII.GetString(name.ToArray());
						material = material.Replace("\0", "");

						materials.Add(material);
					}
				}

				//Unknown (short) * 20
				modelDataBlock.AddRange(br.ReadBytes(16));

				if(unk5 > 0)
				{
					modelDataBlock.AddRange(br.ReadBytes(unk5 * 32));
				}

				//LoD Section
				List<LevelOfDetail> lodList = new List<LevelOfDetail>();
				List<LevelOfDetail> OriginalLodList = new List<LevelOfDetail>();
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

					List<byte> LoDChunk = new List<byte>();
					//LoD UNK
					LoDChunk.AddRange(br.ReadBytes(28));
					br.ReadBytes(4);
					LoDChunk.AddRange(br.ReadBytes(8));

					//LoD Vetex Buffer Size (int)
					if (i == 0)
					{
						int originalSize = br.ReadInt32();
						OriginalLodList[i].VertexDataSize = originalSize;
						int vertSize = 0;
						int vDataSize = 20;
						int data2Stride = 36;

						if (type.Equals("weapon") || type.Equals("monster"))
						{
							vDataSize = 16;
						}

						for(int m = 0; m < cmdList.Count; m++)
						{
							var mg = cmdList[m].meshGeometry;

							if (extraIndexData.totalExtraCounts != null && extraIndexData.totalExtraCounts.ContainsKey(m))
							{
								vertSize += ((mg.Positions.Count + extraIndexData.totalExtraCounts[m]) * vDataSize) + ((mg.Positions.Count + extraIndexData.totalExtraCounts[m]) * data2Stride);
							}
							else
							{
								vertSize += (mg.Positions.Count * vDataSize) + (mg.Positions.Count * data2Stride);
							}
						}

						lod.VertexDataSize = vertSize;
					}
					else
					{
						lod.VertexDataSize = br.ReadInt32();
						OriginalLodList[i].VertexDataSize = lod.VertexDataSize;

					}

					//LoD Index Buffer Size (int)
					if (i == 0)
					{
						int originalSize = br.ReadInt32();
						OriginalLodList[i].IndexDataSize = originalSize;


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
					}
					else
					{
						lod.IndexDataSize = br.ReadInt32();
						OriginalLodList[i].IndexDataSize = lod.IndexDataSize;

					}

					//LoD Vertex Offset (int)
					if (i == 0)
					{
						lod.VertexOffset = br.ReadInt32();
						OriginalLodList[i].VertexOffset = lod.VertexOffset;

					}
					else
					{
						var originalVertexOffset = br.ReadInt32();
						OriginalLodList[i].VertexOffset = originalVertexOffset;

						lod.VertexOffset = lodList[i - 1].VertexOffset + lodList[i - 1].VertexDataSize + lodList[i - 1].IndexDataSize;
					}

					//LoD Index Offset (int)

					if (i == 0)
					{
						var originalIndexOffset = br.ReadInt32();
						OriginalLodList[i].IndexOffset = originalIndexOffset;
						lod.IndexOffset = lod.VertexOffset + lod.VertexDataSize;
						LoDChunk.InsertRange(28, BitConverter.GetBytes(lod.IndexOffset));
					}
					else
					{
						var originalIndexOffset = br.ReadInt32();
						OriginalLodList[i].IndexOffset = originalIndexOffset;
						lod.IndexOffset = lod.VertexOffset + lod.VertexDataSize;
						LoDChunk.InsertRange(28, BitConverter.GetBytes(lod.IndexOffset));
					}

					//LoD Mesh Offset (short)
					modelDataBlock.AddRange(BitConverter.GetBytes((short)lod.MeshOffset));

					//LoD Mesh Count (short)
					modelDataBlock.AddRange(BitConverter.GetBytes((short)lod.MeshCount));

					//LoD Chunk 
					modelDataBlock.AddRange(LoDChunk.ToArray());

					//LoD Vetex Buffer Size (int)
					modelDataBlock.AddRange(BitConverter.GetBytes(lod.VertexDataSize));

					//LoD Index Buffer Size (int)
					modelDataBlock.AddRange(BitConverter.GetBytes(lod.IndexDataSize));

					//LoD Vertex Offset (int)
					modelDataBlock.AddRange(BitConverter.GetBytes(lod.VertexOffset));

					//LoD Index Offset (int)
					modelDataBlock.AddRange(BitConverter.GetBytes(lod.IndexOffset));

					lodList.Add(lod);
				}


				//Replace Half with Floats and compress VertexInfo data
				for (int i = 0; i < lodList[0].MeshCount; i++)
				{
					var normType = (136 * i) + 26;
					var bnOffset = (136 * i) + 33;
					var clrOffset = (136 * i) + 41;
					var tcOffset = (136 * i) + 49;
					var tcType = (136 * i) + 50;

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

				//Meshes
				Dictionary<int, List<MeshInfo>> meshInfoDict = new Dictionary<int, List<MeshInfo>>();

				for(int i = 0; i < lodList.Count; i++)
				{
					List<MeshInfo> meshInfoList = new List<MeshInfo>();

					for (int j = 0; j < lodList[i].MeshCount; j++)
					{
						OriginalLodList[i].MeshList.Add(new Mesh());

						MeshInfo meshInfo = new MeshInfo()
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
							VertexSizes = new List<int>(meshInfo.VertexSizes)
						};

						if (i == 0)
						{
							meshInfo.VertexSizes[1] = meshInfo.VertexSizes[1] + 12;

							try
							{
								var mg = cmdList[j].meshGeometry;
								int vc;
								//Vertex Count (int)
								if (extraIndexData.totalExtraCounts != null && extraIndexData.totalExtraCounts.ContainsKey(j))
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

						}
						else
						{
							//Vertex Count (int)
							modelDataBlock.AddRange(BitConverter.GetBytes(meshInfo.VertexCount));

							//Index Count (int)
							modelDataBlock.AddRange(BitConverter.GetBytes(meshInfo.IndexCount));
						}

						//material index (short)
						modelDataBlock.AddRange(BitConverter.GetBytes((short)meshInfo.MaterialNum));

						//mesh part table offset (short)
						modelDataBlock.AddRange(BitConverter.GetBytes((short)meshInfo.MeshPartOffset));

						//mesh part count (short)
						modelDataBlock.AddRange(BitConverter.GetBytes((short)meshInfo.MeshPartCount));

						//bone list index (short)
						modelDataBlock.AddRange(BitConverter.GetBytes((short)meshInfo.BoneListIndex));

						if(j != 0)
						{
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
							//index data offset (int)
							modelDataBlock.AddRange(BitConverter.GetBytes(meshInfo.IndexDataOffset));
						}

						if (i == 0)
						{
							int posCount = 0;
							try
							{
								var mg = cmdList[j].meshGeometry;
								if (extraIndexData.totalExtraCounts != null && extraIndexData.totalExtraCounts.ContainsKey(j))
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

						}
						else
						{
							//vertex data offset[0] (int)
							modelDataBlock.AddRange(BitConverter.GetBytes(meshInfo.VertexDataOffsets[0]));

							//vertex data offset[1] (int)
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

				modelDataBlock.AddRange(br.ReadBytes(atrStringCount * 4));

				if(unk6 > 0)
				{
					modelDataBlock.AddRange(br.ReadBytes(unk6 * 20));
				}

				//Mesh Parts
				List<MeshPart> meshPart = new List<MeshPart>();

				int meshPadd = 0;

				for (int l = 0; l < lodList.Count; l++)
				{
					for (int i = 0; i < meshInfoDict[l].Count; i++)
					{
						var mList = meshInfoDict[l];
						var mPartCount = mList[i].MeshPartCount;

						for (int j = 0; j < mPartCount; j++)
						{
							MeshPart mp = new MeshPart();

							//Index Offset (int)
							if (l == 0 && i == 0)
							{
								if (j != 0)
								{
									br.ReadBytes(4);
									mp.IndexOffset = meshPart[meshPart.Count - 1].IndexOffset + meshPart[meshPart.Count - 1].IndexCount;
									modelDataBlock.AddRange(BitConverter.GetBytes(mp.IndexOffset));
								}
								else
								{
									mp.IndexOffset = br.ReadInt32();
									modelDataBlock.AddRange(BitConverter.GetBytes(mp.IndexOffset));
								}
							}
							else if (l == 0 && i == 1)
							{
								br.ReadBytes(4);
								if (j == 0)
								{
									mp.IndexOffset = meshPart[meshPart.Count - 1].IndexOffset + meshPart[meshPart.Count - 1].IndexCount + meshPadd;
								}
								else
								{
									mp.IndexOffset = meshPart[meshPart.Count - 1].IndexOffset + meshPart[meshPart.Count - 1].IndexCount;
								}

								modelDataBlock.AddRange(BitConverter.GetBytes(mp.IndexOffset));
							}
							else
							{
								mp.IndexOffset = br.ReadInt32();
								modelDataBlock.AddRange(BitConverter.GetBytes(mp.IndexOffset));
							}

							//Index Count (int)
							if (l == 0 && i < cmdList.Count)
							{
								var partsDict = cmdList[i].partsDict;

								var indexCount = br.ReadInt32();
								if (partsDict.ContainsKey(j))
								{
									mp.IndexCount = partsDict[j];
								}
								else
								{
									mp.IndexCount = 0;
								}

								modelDataBlock.AddRange(BitConverter.GetBytes(mp.IndexCount));
							}
							else if (l == 0 && i >= cmdList.Count)
							{
								var indexCount = br.ReadInt32();
								mp.IndexCount = 0;
								modelDataBlock.AddRange(BitConverter.GetBytes(mp.IndexCount));
							}
							else
							{
								mp.IndexCount = br.ReadInt32();
								modelDataBlock.AddRange(BitConverter.GetBytes(mp.IndexCount));
							}

							if (l == 0 && i == 0)
							{
								if (j == mPartCount - 1)
								{
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
							}

						    //Attributes (int)
                            mp.Attributes = br.ReadInt32();

                            if (importSettings != null && importSettings.ContainsKey(i.ToString()) && l == 0)
                            {
                                if (importSettings[i.ToString()].PartDictionary != null)
                                {
                                    var attr = importSettings[i.ToString()].PartDictionary[j];
                                    modelDataBlock.AddRange(BitConverter.GetBytes(attr));
                                }
                                else
                                {
                                    modelDataBlock.AddRange(BitConverter.GetBytes(mp.Attributes));
                                }
                            }
                            else
						    {
						        modelDataBlock.AddRange(BitConverter.GetBytes(mp.Attributes));
                            }

                            //Bone reference offset (short)
                            mp.BoneOffset = br.ReadInt16();
							modelDataBlock.AddRange(BitConverter.GetBytes((short)mp.BoneOffset));

							//Bone reference count (short)
							mp.BoneCount = br.ReadInt16();
							modelDataBlock.AddRange(BitConverter.GetBytes((short)mp.BoneCount));

							meshPart.Add(mp);
						}
					}
				}

				if (unk12 > 0)
				{
					modelDataBlock.AddRange(br.ReadBytes(unk12 * 12));
				}

				modelDataBlock.AddRange(br.ReadBytes(matStringCount * 4));

				modelDataBlock.AddRange(br.ReadBytes(boneStringCount * 4));

				for (int i = 0; i < boneListCount; i++)
				{
					//bone list
					modelDataBlock.AddRange(br.ReadBytes(128));

					//bone count (int)
					modelDataBlock.AddRange(br.ReadBytes(4));
				}

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


				var pCount = 0;
				var pCount1 = 0;
				var pCount2 = 0;

				if (unk1 > 0)
				{
					for (int i = 0; i < unk1; i++)
					{
						//not sure
						modelDataBlock.AddRange(br.ReadBytes(4));

						//LoD[0] Extra Data Index
						var p1 = br.ReadUInt16();
						modelDataBlock.AddRange(BitConverter.GetBytes(p1));

						//LoD[1] Extra Data Index
						var p2 = br.ReadUInt16();
						modelDataBlock.AddRange(BitConverter.GetBytes(p2));

						//LoD[2] Extra Data Index
						var p3 = br.ReadUInt16();
						modelDataBlock.AddRange(BitConverter.GetBytes(p3));

						//LoD[0] Extra Data Part Count
						var p1n = br.ReadUInt16();
						modelDataBlock.AddRange(BitConverter.GetBytes(p1n));
						pCount += p1n;

						//LoD[1] Extra Data Part Count
						var p2n = br.ReadUInt16();
						modelDataBlock.AddRange(BitConverter.GetBytes(p2n));
						pCount1 += p2n;

						//LoD[2] Extra Data Part Count
						var p3n = br.ReadUInt16();
						modelDataBlock.AddRange(BitConverter.GetBytes(p3n));
						pCount2 += p3n;

					}
				}

				if (unk1 > 0)
				{
					for (int i = 0; i < modelData.LoD[0].MeshCount; i++)
					{
						var ido = OriginalLodList[0].MeshList[i].MeshInfo.IndexDataOffset;
						indexLoc.Add(ido, i);
					}
				}


				List<int> maskCounts = new List<int>();
				Dictionary<int, int> totalExtraCounts = new Dictionary<int, int>();
				if (unk2 > 0)
				{
					for (int i = 0; i < pCount; i++)
					{
						//Index Offset Start
						var m1 = br.ReadInt32();
						var iLoc = indexLoc[m1];
						modelDataBlock.AddRange(BitConverter.GetBytes(nOffsets[iLoc]));

						//index count
						var mCount = br.ReadInt32();
						modelDataBlock.AddRange(BitConverter.GetBytes(mCount));

						//index offset in unk3
						var mOffset = br.ReadInt32();
						modelDataBlock.AddRange(BitConverter.GetBytes(mOffset));


						indexCounts.Add(new ExtraIndex() { IndexLocation = iLoc, IndexCount = mCount });
						maskCounts.Add(mCount);
					}

					var remaining = br.ReadBytes((pCount1 + pCount2) * 12);
					modelDataBlock.AddRange(remaining);
				}

				int totalLoD0MaskCount = 0;

				if (unk2 > 0)
				{
					for (int i = 0; i < pCount; i++)
					{
						totalLoD0MaskCount += maskCounts[i];
					}
				}

				if (unk3 > 0)
				{
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

						for(int i = 0; i < ic.IndexCount; i++)
						{
							var oIndex = ic.IndexList[i][0];

							var mIndex = ic.IndexList[i][1];
							short nIndex = 0;
							if(mIndex != 0)
							{
								short sub = (short)(mIndex - iMin);
								nIndex = (short)(sub + iMax + 1);
							}

							modelDataBlock.AddRange(BitConverter.GetBytes(oIndex));
							modelDataBlock.AddRange(BitConverter.GetBytes(nIndex));
						}
						m++;
					}

					//the rest of unk3
					modelDataBlock.AddRange(br.ReadBytes(unk3Remainder));
				}

				//Bone index count (int)
				int boneIndexCount = br.ReadInt32();
				modelDataBlock.AddRange(BitConverter.GetBytes(boneIndexCount));

				//Bone indices
				modelDataBlock.AddRange(br.ReadBytes(boneIndexCount));

				//Padding count
				byte paddingCount = br.ReadByte();
				br.ReadBytes(paddingCount);
				modelDataBlock.Add(paddingCount);
				modelDataBlock.AddRange(new byte[paddingCount]);

				//Bounding Boxes
				modelDataBlock.AddRange(br.ReadBytes(128));

				//Bones
				modelDataBlock.AddRange(br.ReadBytes(boneStringCount * 32));

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

				for(int i = 0; i < modelDataParts; i++)
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


				/*
				 * ---------------------
				 * Vertex Data Start
				 * ---------------------
				*/
				List<int> compMeshSizes = new List<int>();
				List<int> compIndexSizes = new List<int>();

				List<DataBlocks> dbList = new List<DataBlocks>();
				DataBlocks db = new DataBlocks();

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

						importDict[extraLoc].dataSet1.AddRange(maskVerts);

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

							importDict[extraLoc].dataSet2.AddRange(BitConverter.GetBytes(x));
							importDict[extraLoc].dataSet2.AddRange(BitConverter.GetBytes(y));
							importDict[extraLoc].dataSet2.AddRange(BitConverter.GetBytes(z));

							importDict[extraLoc].dataSet2.AddRange(br.ReadBytes(4));
							importDict[extraLoc].dataSet2.AddRange(br.ReadBytes(4));

							//texCoord
							h1 = System.Half.ToHalf((ushort)br.ReadInt16());
							h2 = System.Half.ToHalf((ushort)br.ReadInt16());
							h3 = System.Half.ToHalf((ushort)br.ReadInt16());
							h4 = System.Half.ToHalf((ushort)br.ReadInt16());

							x = h1;
							y = h2;
							z = h3;
							w = h4;

							importDict[extraLoc].dataSet2.AddRange(BitConverter.GetBytes(x));
							importDict[extraLoc].dataSet2.AddRange(BitConverter.GetBytes(y));
							importDict[extraLoc].dataSet2.AddRange(BitConverter.GetBytes(z));
							importDict[extraLoc].dataSet2.AddRange(BitConverter.GetBytes(w));
						}
					}
				}

				for (int i = 0; i < importDict.Count; i++)
				{
					db.VertexDataBlock.AddRange(importDict[i].dataSet1);
					db.VertexDataBlock.AddRange(importDict[i].dataSet2);
					db.IndexDataBlock.AddRange(importDict[i].indexSet);

					var indexPadd = importDict[i].indexSet.Count % 16;
					if(indexPadd != 0)
					{
						db.IndexDataBlock.AddRange(new byte[16 - indexPadd]);
					}
				}

				db.VDBParts = (int)Math.Ceiling(db.VertexDataBlock.Count / 16000f);
				int[] VDB1PartCounts = new int[db.VDBParts];
				int VDB1Remaining = db.VertexDataBlock.Count;

				for (int i = 0; i < db.VDBParts; i++)
				{

					if (VDB1Remaining >= 16000)
					{
						VDB1PartCounts[i] = 16000;
						VDB1Remaining -= 16000;
					}
					else
					{
						VDB1PartCounts[i] = VDB1Remaining;
					}
				}

				for (int i = 0; i < db.VDBParts; i++)
				{
					var compVertexData1 = Compressor(db.VertexDataBlock.GetRange(i * 16000, VDB1PartCounts[i]).ToArray());

					compressedData.AddRange(BitConverter.GetBytes(16));
					compressedData.AddRange(BitConverter.GetBytes(0));
					compressedData.AddRange(BitConverter.GetBytes(compVertexData1.Length));
					compressedData.AddRange(BitConverter.GetBytes(VDB1PartCounts[i]));
					compressedData.AddRange(compVertexData1);

					var vertexPadding = 128 - ((compVertexData1.Length + 16) % 128);

					compressedData.AddRange(new byte[vertexPadding]);

					db.compVertexDataBlockSize += compVertexData1.Length + 16 + vertexPadding;
					compMeshSizes.Add(compVertexData1.Length + 16 + vertexPadding);
				}


				db.IDBParts = (int)Math.Ceiling(db.IndexDataBlock.Count / 16000f);
				int[] IDB1PartCounts = new int[db.IDBParts];
				int IDB1Remaining = db.IndexDataBlock.Count;

				for (int i = 0; i < db.IDBParts; i++)
				{

					if (IDB1Remaining >= 16000)
					{
						IDB1PartCounts[i] = 16000;
						IDB1Remaining -= 16000;
					}
					else
					{
						IDB1PartCounts[i] = IDB1Remaining;
					}
				}

				var indexPadding = 0;

				for(int i = 0; i < db.IDBParts; i++)
				{
					var compIndexData1 = Compressor(db.IndexDataBlock.GetRange(i * 16000, IDB1PartCounts[i]).ToArray());

					compressedData.AddRange(BitConverter.GetBytes(16));
					compressedData.AddRange(BitConverter.GetBytes(0));
					compressedData.AddRange(BitConverter.GetBytes(compIndexData1.Length));
					compressedData.AddRange(BitConverter.GetBytes(IDB1PartCounts[i]));
					compressedData.AddRange(compIndexData1);

					indexPadding = 128 - ((compIndexData1.Length + 16) % 128);

					compressedData.AddRange(new byte[indexPadding]);

					db.compIndexDataBlockSize += compIndexData1.Length + 16 + indexPadding;
					compIndexSizes.Add(compIndexData1.Length + 16 + indexPadding);
				}

				dbList.Add(db);




				for(int i = 1; i < 3; i++)
				{
					db = new DataBlocks();

					for (int j = 0; j < lodList[i].MeshCount; j++)
					{
						var lod = OriginalLodList[i];
						var meshInfo = lod.MeshList[j].MeshInfo;


						br.BaseStream.Seek(lod.VertexOffset + meshInfo.VertexDataOffsets[0], SeekOrigin.Begin);

						var p1 = meshInfo.VertexCount * meshInfo.VertexSizes[0];
						var p2 = meshInfo.VertexCount * meshInfo.VertexSizes[1];
						db.VertexDataBlock.AddRange(br.ReadBytes(p1));
						db.VertexDataBlock.AddRange(br.ReadBytes(p2));
					}

					for (int j = 0; j < lodList[i].MeshCount; j++)
					{
						var lod = OriginalLodList[i];
						var meshInfo = lod.MeshList[j].MeshInfo;

						br.BaseStream.Seek(lod.IndexOffset + (meshInfo.IndexDataOffset), SeekOrigin.Begin);

						db.IndexDataBlock.AddRange(br.ReadBytes(meshInfo.IndexCount * 2));

						indexPadding = (meshInfo.IndexCount * 2) % 16;
						if (indexPadding != 0)
						{
							db.IndexDataBlock.AddRange(new byte[16 - indexPadding]);

							if(j != lodList[i].MeshCount - 1)
							{
								br.ReadBytes(16 - indexPadding);
							}
						}
					}

					db.VDBParts = (int)Math.Ceiling(db.VertexDataBlock.Count / 16000f);
					int[] VDB2PartCounts = new int[db.VDBParts];
					int VDB2Remaining = db.VertexDataBlock.Count;

					for (int j = 0; j < db.VDBParts; j++)
					{

						if (VDB2Remaining >= 16000)
						{
							VDB2PartCounts[j] = 16000;
							VDB2Remaining -= 16000;
						}
						else
						{
							VDB2PartCounts[j] = VDB2Remaining;
						}
					}

					for (int j = 0; j < db.VDBParts; j++)
					{
						var compVertexData2 = Compressor(db.VertexDataBlock.GetRange(j * 16000, VDB2PartCounts[j]).ToArray());

						compressedData.AddRange(BitConverter.GetBytes(16));
						compressedData.AddRange(BitConverter.GetBytes(0));
						compressedData.AddRange(BitConverter.GetBytes(compVertexData2.Length));
						compressedData.AddRange(BitConverter.GetBytes(VDB2PartCounts[j]));
						compressedData.AddRange(compVertexData2);

						var vertexPadding = 128 - ((compVertexData2.Length + 16) % 128);

						compressedData.AddRange(new byte[vertexPadding]);

						db.compVertexDataBlockSize += compVertexData2.Length + 16 + vertexPadding;
						compMeshSizes.Add(compVertexData2.Length + 16 + vertexPadding);
					}


					var compIndexData2 = Compressor(db.IndexDataBlock.ToArray());

					compressedData.AddRange(BitConverter.GetBytes(16));
					compressedData.AddRange(BitConverter.GetBytes(0));
					compressedData.AddRange(BitConverter.GetBytes(compIndexData2.Length));
					compressedData.AddRange(BitConverter.GetBytes(db.IndexDataBlock.Count));
					compressedData.AddRange(compIndexData2);

					indexPadding = 128 - ((compIndexData2.Length + 16) % 128);

					compressedData.AddRange(new byte[indexPadding]);

					db.compIndexDataBlockSize += compIndexData2.Length + 16 + indexPadding;

					dbList.Add(db);
				}

				/*
				 * -----------------------------------
				 * Create Header Start
				 * -----------------------------------
				 */
				int headerLength = 256;

				if ((compMeshSizes.Count + modelDataParts + 3 + compIndexSizes.Count) > 24)
				{
					headerLength = 384;
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
				var vert3 = ind2 + 1;
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
				datHeader.AddRange(BitConverter.GetBytes((short)1));
				//Index Data Block LoD[3] part count
				datHeader.AddRange(BitConverter.GetBytes((short)1));

				//Mesh Count
				datHeader.AddRange(BitConverter.GetBytes((short)meshCount));
				//Material Count
				datHeader.AddRange(BitConverter.GetBytes((short)matStringCount));
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

				//Vertex Data Block LoD[1] part padded sizes
				for (int i = 0; i < dbList[0].VDBParts; i++)
				{
					datHeader.AddRange(BitConverter.GetBytes((short)compMeshSizes[i]));
				}
				VDBPartCount += dbList[0].VDBParts;
				//Index Data Block LoD[1] padded size
				for(int i = 0; i < dbList[0].IDBParts; i++)
				{
					datHeader.AddRange(BitConverter.GetBytes((short)compIndexSizes[i]));
				}

				//Vertex Data Block LoD[2] part padded sizes
				for (int i = 0; i < dbList[1].VDBParts; i++)
				{
					datHeader.AddRange(BitConverter.GetBytes((short)compMeshSizes[VDBPartCount + i]));
				}
				VDBPartCount += dbList[1].VDBParts;
				//Index Data Block LoD[2] padded size
				datHeader.AddRange(BitConverter.GetBytes((short)dbList[1].compIndexDataBlockSize));

				//Vertex Data Block LoD[3] part padded sizes
				for (int i = 0; i < dbList[2].VDBParts; i++)
				{
					datHeader.AddRange(BitConverter.GetBytes((short)compMeshSizes[VDBPartCount + i]));
				}

				//Index Data Block LoD[3] padded size
				datHeader.AddRange(BitConverter.GetBytes((short)dbList[2].compIndexDataBlockSize));

				//Rest of header
				if(datHeader.Count != 256 && datHeader.Count != 384)
				{
					var headerEnd = headerLength - (datHeader.Count % headerLength);
					datHeader.AddRange(new byte[headerEnd]);
				}
			}
			compressedData.InsertRange(0, datHeader);

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

			public List<float> vertex = new List<float>();
			public List<float> normal = new List<float>();
			public List<float> texCoord = new List<float>();
			public List<float> texCoord2 = new List<float>();
			public List<float> weights = new List<float>();
			public List<float> biNormal = new List<float>();
			public List<float> tangent = new List<float>();
			public List<int> index = new List<int>();
			public List<int> bIndex = new List<int>();
			public List<int> vCount = new List<int>();

			public List<int> vIndexList = new List<int>();
			public List<int> nIndexList = new List<int>();
			public List<int> bnIndexList = new List<int>();
			public List<int> tcIndexList = new List<int>();
			public List<int> tc2IndexList = new List<int>();

			public Dictionary<int, int> partsDict = new Dictionary<int, int>();
		}

		public class ColladaMeshData
		{
			public MeshGeometry3D meshGeometry = new MeshGeometry3D();

			public List<byte[]> blendIndices = new List<byte[]>();
			public List<byte[]> blendWeights = new List<byte[]>();
			public List<int> handedness = new List<int>();
			public Vector2Collection texCoord2 = new Vector2Collection();

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
		        while (fileLength >= 2000000000)
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
