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
using System.IO;
using System.Windows.Media.Imaging;
using Media3D = System.Windows.Media.Media3D;
using Transform3D = System.Windows.Media.Media3D.Transform3D;
using TranslateTransform3D = System.Windows.Media.Media3D.TranslateTransform3D;

namespace FFXIV_TexTools2.ViewModel
{
    public class Composite3DViewModel : BaseViewModel, INotifyPropertyChanged
    {
        bool modelRendering = true, secondModelRendering = true, thirdModelRendering = true;
        Vector3 light1Direction, light2Direction;

        public string Name { get; set; }
        public Composite3DViewModel ViewModel { get { return this; } }

        public MeshGeometry3D Model { get; private set; }
        public MeshGeometry3D SecondModel { get; private set; }
        public MeshGeometry3D ThirdModel { get; private set; }

        public CustomPhongMaterial ModelMaterial { get; set; }
        public CustomPhongMaterial SecondModelMaterial { get; set; }
        public CustomPhongMaterial ThirdModelMaterial { get; set; }


        public Transform3D ModelTransform { get; private set; }
        public Transform3D SecondModelTransform { get; private set; }
        public Transform3D ThirdModelTransform { get; private set; }

        public Vector3 Light1Direction { get { return light1Direction; } set { light1Direction = value; NotifyPropertyChanged("Light1Direction"); } }
        public Vector3 Light2Direction { get { return light2Direction; } set { light2Direction = value; NotifyPropertyChanged("Light2Direction"); } }
        public Color4 Light1Color { get; set; }

        public Color4 AmbientLightColor { get; set; }
        public Color4 BackgroundColor { get; set; }

        public bool RenderLight1 { get; set; }
        public bool RenderLight2 { get; set; }

        public bool ModelRendering { get { return modelRendering; } set { modelRendering = value; NotifyPropertyChanged("ModelRendering"); } }
        public bool SecondModelRendering { get { return secondModelRendering; } set { secondModelRendering = value; NotifyPropertyChanged("SecondModelRendering"); } }
        public bool ThirdModelRendering { get { return thirdModelRendering; } set { thirdModelRendering = value; NotifyPropertyChanged("ThirdModelRendering"); } }

        bool second = false;
        bool disposed = false;
        bool disposing = false;

        List<MDLTEXData> mData;
        Stream diffuse, normal, colorTable, mask, specular, alpha;

        /// <summary>
        /// The Default View Model for the 3D content
        /// </summary>
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

        /// <summary>
        /// Sets up the View Model for the given mesh data
        /// </summary>
        /// <param name="meshData">The models data</param>
        public Composite3DViewModel(List<MDLTEXData> meshData)
        {
            mData = meshData;

            for(int i = 0; i < meshData.Count; i++)
            {
                setModel(i);
            }

            RenderTechniquesManager = new CustomRenderTechniquesManager();
            RenderTechnique = RenderTechniquesManager.RenderTechniques["RenderCustom"];
            EffectsManager = new CustomEffectsManager(RenderTechniquesManager);

            Vector3 center;
            try
            {
                center = SecondModel.BoundingSphere.Center;
            }
            catch
            {
                center = Model.BoundingSphere.Center;
            }


            Camera = new PerspectiveCamera { Position = new Media3D.Point3D(center.X, center.Y, center.Z + 2), LookDirection = new Media3D.Vector3D(0, 0, center.Z - 2) };           


            BackgroundColor = Color.Gray;
            this.AmbientLightColor = new Color4(0.1f, 0.1f, 0.1f, 1.0f);

            this.Light1Direction = new Vector3(0, 0, -1f);
            this.Light2Direction = new Vector3(0, 0, 1f);
            this.Light1Color = Color.White;
            this.RenderLight1 = true;
            this.RenderLight2 = true;


            this.ModelTransform = new TranslateTransform3D(0, 0, 0);
            this.SecondModelTransform = new TranslateTransform3D(0, 0, 0);
            this.ThirdModelTransform = new TranslateTransform3D(0, 0, 0);
        }

        private void setModel(int m)
        {
            MeshGeometryModel3D mgm3d = new MeshGeometryModel3D();
            MeshGeometry3D mg = new MeshGeometry3D();
            mg.Positions = mData[m].Mesh.Vertices;
            mg.Indices = mData[m].Mesh.Indices;
            mg.Normals = mData[m].Mesh.Normals;
            mg.TextureCoordinates = mData[m].Mesh.TextureCoordinates;
            mg.Colors = mData[m].Mesh.VertexColors;
            MeshBuilder.ComputeTangents(mg);
            mgm3d.Geometry = mg;

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

            colorTable = null;
            if (mData[m].ColorTable != null)
            {
                colorTable = new MemoryStream();
                var enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(mData[m].ColorTable));
                enc.Save(colorTable);
            }

            mask = null;
            if (mData[m].Mask != null)
            {
                mask = new MemoryStream();
                BitmapEncoder enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(mData[m].Mask));
                enc.Save(mask);
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

            if (mData.Count > 1)
            {
                float specularShine = 30f;

                if (mData[m].IsBody || mData[m].IsFace)
                {
                    this.Model = mg;

                    ModelMaterial = new CustomPhongMaterial()
                    {
                        DiffuseColor = PhongMaterials.ToColor(1, 1, 1, 1),
                        SpecularShininess = specularShine,
                        NormalMap = normal,
                        MaskMap = mask,
                        DiffuseMap = diffuse,
                        DiffuseAlphaMap = alpha,
                        ColorTable = colorTable,
                        SpecularMap = specular
                    };

                }
                else if (!mData[m].IsBody && !second)
                {
                    this.SecondModel = mg;

                    SecondModelMaterial = new CustomPhongMaterial()
                    {
                        DiffuseColor = PhongMaterials.ToColor(1, 1, 1, 1),
                        SpecularShininess = specularShine,
                        NormalMap = normal,
                        MaskMap = mask,
                        ColorTable = colorTable,
                        DiffuseMap = diffuse,
                        DiffuseAlphaMap = alpha,
                        SpecularMap = specular
                    };

                    second = true;
                }
                else if(!mData[m].IsBody)
                {
                    this.ThirdModel = mg;
                    ThirdModelMaterial = new CustomPhongMaterial()
                    {
                        DiffuseColor = PhongMaterials.ToColor(1, 1, 1, 1),
                        SpecularShininess = specularShine,
                        NormalMap = normal,
                        MaskMap = mask,
                        ColorTable = colorTable,
                        DiffuseMap = diffuse,
                        DiffuseAlphaMap = alpha,
                        SpecularMap = specular
                    };
                }
            }
            else
            {
                this.SecondModel = mg;

                SecondModelMaterial = new CustomPhongMaterial()
                {
                    DiffuseColor = PhongMaterials.ToColor(1, 1, 1, 1),
                    SpecularShininess = 30f,
                    NormalMap = normal,
                    MaskMap = mask,
                    ColorTable = colorTable,
                    SpecularMap = specular,
                    DiffuseMap = diffuse,
                    DiffuseAlphaMap = alpha
                };
            }
        }

        /// <summary>
        /// Sets which models are to be rendered
        /// </summary>
        /// <param name="selected">The currently selected mesh</param>
        public void Rendering(string selected)
        {
            if (selected.Equals(Strings.All))
            {
                ModelRendering = true;
                SecondModelRendering = true;
                ThirdModelRendering = true;
            }
            else
            {
               var selectedMesh = int.Parse(selected);

                if(mData.Count >= 3)
                {
                    if (selectedMesh == 0)
                    {
                        ModelRendering = true;
                        SecondModelRendering = false;
                        ThirdModelRendering = false;
                    }
                    else if (selectedMesh == 1)
                    {
                        ModelRendering = false;
                        SecondModelRendering = true;
                        ThirdModelRendering = false;
                    }
                    else if (selectedMesh == 2)
                    {
                        ModelRendering = false;
                        SecondModelRendering = false;
                        ThirdModelRendering = true;
                    }
                }
                else
                {
                    if (selectedMesh == 0)
                    {
                        ModelRendering = true;
                        SecondModelRendering = false;
                    }
                    else if (selectedMesh == 1)
                    {

                        ModelRendering = false;
                        SecondModelRendering = true;
                    }
                }

            }
        }

        /// <summary>
        /// Sets the transparency for the model
        /// </summary>
        public void Transparency()
        {
            if(SecondModelMaterial.DiffuseColor.Alpha == 1)
            {
                SecondModelMaterial.DiffuseColor = PhongMaterials.ToColor(1, 1, 1, .4);
                SecondModel.UpdateVertices();

                if (ThirdModel != null)
                {
                    ThirdModelMaterial.DiffuseColor = PhongMaterials.ToColor(1, 1, 1, .4);
                    ThirdModel.UpdateVertices();
                }

            }
            else
            {
                SecondModelMaterial.DiffuseColor = PhongMaterials.ToColor(1, 1, 1, 1);
                SecondModel.UpdateVertices();

                if (ThirdModel != null)
                {
                    ThirdModelMaterial.DiffuseColor = PhongMaterials.ToColor(1, 1, 1, 1);
                    ThirdModel.UpdateVertices();
                }
            }
        }

        /// <summary>
        /// Changes the Specular Shininess of the model
        /// </summary>
        /// <param name="itemName">The currently selected items name</param>
        public void Reflections(string itemName)
        {
            if (itemName.Equals(Strings.Face) || itemName.Equals(Strings.Face))
            {
                if (ModelMaterial.SpecularShininess < 10)
                {
                    ModelMaterial.SpecularShininess += 1;
                    Model.UpdateVertices();
                }
                else if (ModelMaterial.SpecularShininess < 50)
                {
                    ModelMaterial.SpecularShininess += 10;
                    Model.UpdateVertices();
                }
                else
                {
                    ModelMaterial.SpecularShininess = 1;
                    Model.UpdateVertices();
                }
            }
            else
            {
                if (SecondModelMaterial.SpecularShininess < 10)
                {
                    SecondModelMaterial.SpecularShininess += 1;
                    SecondModel.UpdateVertices();

                    if (ThirdModel != null)
                    {
                        ThirdModelMaterial.SpecularShininess += 1;
                        ThirdModel.UpdateVertices();
                    }
                }
                else if (SecondModelMaterial.SpecularShininess < 50)
                {
                    SecondModelMaterial.SpecularShininess += 10;
                    SecondModel.UpdateVertices();

                    if (ThirdModel != null)
                    {
                        ThirdModelMaterial.SpecularShininess += 10;
                        ThirdModel.UpdateVertices();
                    }
                }
                else
                {
                    SecondModelMaterial.SpecularShininess = 1;
                    SecondModel.UpdateVertices();

                    if (ThirdModel != null)
                    {
                        ThirdModelMaterial.SpecularShininess = 1;
                        ThirdModel.UpdateVertices();
                    }
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
                    if(diffuse != null)
                    {
                        diffuse.Dispose();
                    }

                    if(normal != null)
                    {
                        normal.Dispose();
                    }

                    if(colorTable != null)
                    {
                        colorTable.Dispose();
                    }

                    if(mask != null)
                    {
                        mask.Dispose();
                    }

                    if(specular != null)
                    {
                        specular.Dispose();
                    }

                    if (ModelMaterial != null)
                    {
                        ModelMaterial.Dispose();
                    }

                    if (SecondModelMaterial != null)
                    {

                        SecondModelMaterial.Dispose();
                    }

                    if (ThirdModelMaterial != null)
                    {
                        ThirdModelMaterial.Dispose();
                    }

                    if (mData != null)
                    {
                        mData.Clear();
                    }
                }
                disposed = true;
            }

            base.Dispose(disposing);

        }

        
        public event PropertyChangedEventHandler PropertyChanged;


        protected void NotifyPropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

    }
}
