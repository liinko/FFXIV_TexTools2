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

using FFXIV_TexTools2.Material;
using HelixToolkit.Wpf.SharpDX;
using SharpDX;
using System.IO;
using System.Windows.Media.Imaging;
using Point3D = System.Windows.Media.Media3D.Point3D;
using Transform3D = System.Windows.Media.Media3D.Transform3D;
using TranslateTransform3D = System.Windows.Media.Media3D.TranslateTransform3D;
using Vector3D = System.Windows.Media.Media3D.Vector3D;

namespace FFXIV_TexTools2.ViewModel
{
    public class MDLViewModel : BaseViewModel
    {
        public string Name { get; set; }
        public MDLViewModel ViewModel { get { return this; } }
        public MeshGeometry3D Model { get; private set; }
        public MeshGeometry3D Sphere { get; private set; }
        public Element3DCollection ModelGeometry { get; set; }

        public PhongMaterial ModelMaterial { get; set; }
        public PhongMaterial LightModelMaterial { get; set; }

        public Vector3 Light1Direction { get; set; }
        public Vector3 Light2Direction { get; set; }
        public Color4 Light1Color { get; set; }

        public Color4 AmbientLightColor { get; set; }
        public Color4 BackgroundColor { get; set; }

        public Transform3D ModelTransform { get; private set; }

        public bool RenderLight1 { get; set; }

        public DefaultRenderTechniquesManager RenderTechniquesManager { get; private set; }
        public Viewport3DX modelView
        {
            get;
            set;
        }

        public MDLViewModel(Mesh mesh, BitmapSource diffuseMap, BitmapSource normalMap, MeshGeometryModel3D mgm, BitmapSource displaceMap)
        {
            RenderTechniquesManager = new DefaultRenderTechniquesManager();
            RenderTechnique = RenderTechniquesManager.RenderTechniques[DefaultRenderTechniqueNames.Phong];
            EffectsManager = new DefaultEffectsManager(RenderTechniquesManager);

            // ----------------------------------------------
            // setup scene
            BackgroundColor = Color.Gray;
            //this.AmbientLightColor = new Color4(0.2f, 0.2f, 0.2f, 1.0f);
            ///this.AmbientLightColor = Color.White;


            this.RenderLight1 = true;

            this.Light1Color = Color.White;

            this.Light1Direction = new Vector3(0, -1, -1);
            this.Light2Direction = new Vector3(0, 1, 1);

            // scene model3d
            MeshGeometryModel3D mgm3d = new MeshGeometryModel3D();
            MeshGeometry3D mg = new MeshGeometry3D();
            mg.Positions = mesh.VertexList;
            mg.Indices = mesh.IndList;
            mg.Normals = mesh.NormalList;
            mg.TextureCoordinates = mesh.CoordList;
            MeshBuilder.ComputeTangents(mg);
            mgm3d.Geometry = mg;
            var bounds = mgm3d.Bounds;
            var c = bounds.Maximum - bounds.Minimum;
            var center = new Vector3(bounds.Minimum.X + c.X / 2, bounds.Minimum.Y + c.Y / 2, bounds.Minimum.Z + c.Z / 2);

            this.Camera = new PerspectiveCamera { Position = new Point3D(center.X, center.Y + 1, center.Z + 2), LookDirection = new Vector3D(center.X, center.Y, center.Z - 2)};

            Stream diffuse = new MemoryStream() ;
            BitmapEncoder enc = new PngBitmapEncoder();
            enc.Frames.Add(BitmapFrame.Create(diffuseMap));
            enc.Save(diffuse);

            Stream normal = new MemoryStream();
            enc = new PngBitmapEncoder();
            enc.Frames.Add(BitmapFrame.Create(normalMap));
            enc.Save(normal);

            this.Model = mg;
            this.ModelMaterial = PhongMaterials.White;
            this.ModelMaterial.DiffuseAlphaMap = diffuse;
            this.ModelMaterial.NormalMap = normal;

            if (displaceMap != null)
            {
                Stream displace = new MemoryStream();
                enc = new PngBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(displaceMap));
                enc.Save(displace);
                this.ModelMaterial.DisplacementMap = displace;
            }

            this.ModelTransform = new TranslateTransform3D(0, 0, 0);
        }
    }
}
