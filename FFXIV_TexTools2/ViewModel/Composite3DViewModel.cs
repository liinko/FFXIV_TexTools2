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

using FFXIV_TexTools2.Model;
using FFXIV_TexTools2.Resources;
using FFXIV_TexTools2.Shader;
using HelixToolkit.Wpf.SharpDX;
using SharpDX;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Media.Imaging;
using Media3D = System.Windows.Media.Media3D;

namespace FFXIV_TexTools2.ViewModel
{
    public class Composite3DViewModel : BaseViewModel, INotifyPropertyChanged
    {
        Vector3 light1Direction, light2Direction;
        ObservableElement3DCollection modelCollection = new ObservableElement3DCollection();
        int currentSS;

        public string Name { get; set; }
        public Composite3DViewModel ViewModel { get { return this; } }

        public Vector3 Light1Direction { get { return light1Direction; } set { light1Direction = value; NotifyPropertyChanged("Light1Direction"); } }
        public Vector3 Light2Direction { get { return light2Direction; } set { light2Direction = value; NotifyPropertyChanged("Light2Direction"); } }
        public Color4 Light1Color { get; set; }

        public Color4 AmbientLightColor { get; set; }
        public Color4 BackgroundColor { get; set; }

        public bool RenderLight1 { get; set; }
        public bool RenderLight2 { get; set; }

        public ObservableElement3DCollection ModelCollection { get { return modelCollection; } set { modelCollection = value; NotifyPropertyChanged("ModelCollection"); } }

        public int CurrentSS { get { return currentSS; } set { currentSS = value; NotifyPropertyChanged("CurrentSS"); } }


        bool second = false;
        bool disposed = false;
        bool disposing = false;

        List<MDLTEXData> mData;
        Stream diffuse, normal, specular, alpha, emissive;

        /// <summary>
        /// Sets up the View Model for the given mesh data
        /// </summary>
        /// <param name="meshData">The models data</param>
        public Composite3DViewModel()
        {
            RenderTechniquesManager = new CustomRenderTechniquesManager();
            RenderTechnique = RenderTechniquesManager.RenderTechniques["RenderCustom"];
            EffectsManager = new CustomEffectsManager(RenderTechniquesManager);

            this.Camera = new PerspectiveCamera();

            BackgroundColor = Color.Gray;
            this.AmbientLightColor = new Color4(0.1f, 0.1f, 0.1f, 1.0f);

            this.Light1Direction = new Vector3(0, 0, -1f);
            this.Light2Direction = new Vector3(0, 0, 1f);
            this.Light1Color = Color.White;
            this.RenderLight1 = true;
            this.RenderLight2 = true;
        }

        public void UpdateModel(List<MDLTEXData> meshData)
        {
            disposed = false;
            disposing = false;

            mData = meshData;

            for (int i = 0; i < meshData.Count; i++)
            {
                ModelCollection.Add(setModel(i));
            }

            Vector3 center = ((CustomGM3D)ModelCollection[0]).Geometry.BoundingSphere.Center;

            Camera.Position = new Media3D.Point3D(center.X, center.Y, center.Z + 2);
            Camera.LookDirection =  new Media3D.Vector3D(0, 0, center.Z - 2);
        }

        private CustomGM3D setModel(int m)
        {
            MeshGeometry3D mg = new MeshGeometry3D();
            mg.Positions = mData[m].Mesh.Vertices;
            mg.Indices = mData[m].Mesh.Indices;
            mg.Normals = mData[m].Mesh.Normals;
            mg.TextureCoordinates = mData[m].Mesh.TextureCoordinates;
            mg.Colors = mData[m].Mesh.VertexColors;

            MeshBuilder.ComputeTangents(mg);

            mg.BiTangents = mData[m].Mesh.BiTangents;
            mData[m].Mesh.Tangents = mg.Tangents;

            CustomGM3D gm3d = new CustomGM3D();

            List<byte> DDS = new List<byte>();

            diffuse = null;
            if (mData[m].Diffuse != null)
            {
                diffuse = new MemoryStream();
                BitmapEncoder enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(mData[m].Diffuse));
                enc.Save(diffuse);
            }

            normal = null;
            if (mData[m].Normal != null)
            {
                normal = new MemoryStream();
                BitmapEncoder enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(mData[m].Normal));
                enc.Save(normal);
            }

            specular = null;
            if (mData[m].Specular != null)
            {
                specular = new MemoryStream();
                var enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(mData[m].Specular));
                enc.Save(specular);
            }

            alpha = null;
            if (mData[m].Alpha != null)
            {
                alpha = new MemoryStream();
                var enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(mData[m].Alpha));
                enc.Save(alpha);
            }

            emissive = null;
            if (mData[m].Emissive != null)
            {
                emissive = new MemoryStream();
                var enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(mData[m].Emissive));
                enc.Save(emissive);
            }

            if (mData.Count > 1)
            {
                float specularShine = 30f;
                CurrentSS = 30;

                if (mData[m].IsBody || mData[m].IsFace)
                {
                    gm3d.Geometry = mg;
                    gm3d.ModelBody = true;

                    gm3d.Material = new CustomPhongMaterial()
                    {
                        DiffuseColor = PhongMaterials.ToColor(1, 1, 1, 1),
                        SpecularShininess = specularShine,
                        NormalMap = normal,
                        DiffuseMap = diffuse,
                        DiffuseAlphaMap = alpha,
                        SpecularMap = specular,
                        EmissiveMap = emissive
                    };
                }
                else if (!mData[m].IsBody && !second)
                {
                    gm3d.Geometry = mg;

                    gm3d.Material = new CustomPhongMaterial()
                    {
                        DiffuseColor = PhongMaterials.ToColor(1, 1, 1, 1),
                        SpecularShininess = specularShine,
                        NormalMap = normal,
                        DiffuseMap = diffuse,
                        DiffuseAlphaMap = alpha,
                        SpecularMap = specular,
                        EmissiveMap = emissive
                    };

                    second = true;
                }
                else if(!mData[m].IsBody)
                {
                    gm3d.Geometry = mg;

                    gm3d.Material = new CustomPhongMaterial()
                    {
                        DiffuseColor = PhongMaterials.ToColor(1, 1, 1, 1),
                        SpecularShininess = specularShine,
                        NormalMap = normal,
                        DiffuseMap = diffuse,
                        DiffuseAlphaMap = alpha,
                        SpecularMap = specular,
                        EmissiveMap = emissive
                    };
                }
            }
            else
            {
                float specularShine = 30f;
                CurrentSS = 30;

                gm3d.Geometry = mg;

                gm3d.Material = new CustomPhongMaterial()
                {
                    DiffuseColor = PhongMaterials.ToColor(1, 1, 1, 1),
                    SpecularShininess = specularShine,
                    NormalMap = normal,
                    SpecularMap = specular,
                    DiffuseMap = diffuse,
                    DiffuseAlphaMap = alpha,
                    EmissiveMap = emissive
                };
            }

            return gm3d;
        }

        /// <summary>
        /// Sets which models are to be rendered
        /// </summary>
        /// <param name="selected">The currently selected mesh</param>
        public void Rendering(string selected)
        {
            if (selected.Equals(Strings.All))
            {
                foreach(var model in ModelCollection)
                {
                    model.IsRendering = true;
                }
            }
            else
            {
               var selectedMesh = int.Parse(selected);

                for(int i = 0; i < ModelCollection.Count; i++)
                {
                    if(i == selectedMesh)
                    {
                        ModelCollection[i].IsRendering = true;
                    }
                    else
                    {
                        ModelCollection[i].IsRendering = false;
                    }

                }
            }
        }

        /// <summary>
        /// Sets the transparency for the model
        /// </summary>
        public void Transparency()
        {
            foreach(var model in ModelCollection)
            {
                if (!((CustomGM3D)model).ModelBody)
                {
                    var material = (CustomPhongMaterial)((CustomGM3D)model).Material;

                    if (material.DiffuseColor.Alpha == 1)
                    {
                        material.DiffuseColor = PhongMaterials.ToColor(1, 1, 1, .4);
                        ((CustomGM3D)model).Geometry.UpdateVertices();
                    }
                    else
                    {
                        material.DiffuseColor = PhongMaterials.ToColor(1, 1, 1, 1);
                        ((CustomGM3D)model).Geometry.UpdateVertices();
                    }
                }
            }
        }

        /// <summary>
        /// Changes the Specular Shininess of the model
        /// </summary>
        /// <param name="itemName">The currently selected items name</param>
        public void Reflections(string itemName)
        {

            foreach(var model in ModelCollection)
            {
                var material = (CustomPhongMaterial)((CustomGM3D)model).Material;

                if (!((CustomGM3D)model).ModelBody || itemName.Equals(Strings.Face))
                {
                    if (material.SpecularShininess < 10)
                    {
                        material.SpecularShininess += 1;
                    }
                    else if (material.SpecularShininess < 50)
                    {
                        material.SpecularShininess += 10;
                    }
                    else
                    {
                        material.SpecularShininess = 1;
                    }
                    ((CustomGM3D)model).Geometry.UpdateVertices();

                    CurrentSS = (int)material.SpecularShininess;
                }
            }
        }

        /// <summary>
        /// Cycles through different lighting for the scene
        /// </summary>
        public void Lighting()
        {
            float x = Light1Direction.X;
            float y = Light1Direction.Y;
            float z = Light1Direction.Z;

            float x1 = Light2Direction.X;
            float y1 = Light2Direction.Y;
            float z1 = Light2Direction.Z;

            if (x > -1)
            {
                Light1Direction = new Vector3((float)Math.Round((x - 0.2f), 2), (float)Math.Round((y - 0.2f), 2), z);
                Light2Direction = new Vector3((float)Math.Round((x1 + 0.2f), 2), (float)Math.Round((y1 - 0.2f), 2), z1);
            }
            else
            {
                Light1Direction = new Vector3(1, 1, -1f);
                Light2Direction = new Vector3(-1, 1, 1f);
            }
        }

        /// <summary>
        /// Disposes of the model
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    if (ModelCollection != null)
                    {
                        foreach (var model in modelCollection)
                        {
                            ((CustomPhongMaterial)((CustomGM3D)model).Material).Dispose();
                            ((CustomGM3D)model).Detach();
                            ((CustomGM3D)model).Dispose();
                            model.Dispose();
                        }

                        foreach (var model in ModelCollection)
                        {
                            ((CustomPhongMaterial)((CustomGM3D)model).Material).Dispose();
                            ((CustomGM3D)model).Detach();
                            ((CustomGM3D)model).Dispose();
                            model.Dispose();
                        }

                        modelCollection.Clear();
                        ModelCollection.Clear();
                    }

                    if (diffuse != null)
                    {
                        diffuse.Dispose();
                    }

                    if(normal != null)
                    {
                        normal.Dispose();
                    }

                    if(specular != null)
                    {
                        specular.Dispose();
                    }

                    if (mData != null)
                    {
                        mData.Clear();
                        mData = null;
                    }
                }
                disposed = true;

                base.Dispose(disposing);
            }
        }
      
        public event PropertyChangedEventHandler PropertyChanged;


        protected void NotifyPropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

    }
}
