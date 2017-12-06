// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CustomPhongMaterial.cs" company="Helix Toolkit">
//   Copyright (c) 2014 Helix Toolkit contributors
// </copyright>
// <summary>
//   Implments a phong-material with its all properties
//   Includes Diffuse, Normal, Displacement, Specular, etc. maps
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace HelixToolkit.Wpf.SharpDX
{
    using global::SharpDX;
    using HelixToolkit.Wpf.SharpDX.Utilities;
    using System;
    using System.ComponentModel;
    using System.IO;
    using System.Windows;

    /// <summary>
    /// Implments a phong-material with its all properties
    /// Includes Diffuse, Normal, Displacement, Specular, etc. maps
    /// </summary>
    [Serializable]
    public partial class CustomPhongMaterial : Material, IDisposable
    {
        /// <summary>
        /// Identifies the System.Windows.Media.Media3D.DiffuseMaterial.AmbientColor�dependency
        /// property.
        /// </summary>
        public static readonly DependencyProperty AmbientColorProperty =
            DependencyProperty.Register("AmbientColor", typeof(Color4), typeof(CustomPhongMaterial), new AffectsRenderPropertyMetadata((Color4)Color.Gray,
                (d, e) =>
                {
                    (d as CustomPhongMaterial).AmbientColorInternal = (Color4)e.NewValue;
                }));

        /// <summary>
        /// Identifies the System.Windows.Media.Media3D.DiffuseMaterial.Color�dependency
        /// property.
        /// </summary>
        public static readonly DependencyProperty DiffuseColorProperty =
            DependencyProperty.Register("DiffuseColor", typeof(Color4), typeof(CustomPhongMaterial), new AffectsRenderPropertyMetadata((Color4)Color.Gray,
                (d, e) =>
                {
                    (d as CustomPhongMaterial).DiffuseColorInternal = (Color4)e.NewValue;
                }));

        /// <summary>
        ///         
        /// </summary>
        public static readonly DependencyProperty EmissiveColorProperty =
            DependencyProperty.Register("EmissiveColor", typeof(Color4), typeof(CustomPhongMaterial), new AffectsRenderPropertyMetadata((Color4)Color.Black,
                (d, e) =>
                {
                    (d as CustomPhongMaterial).EmissiveColorInternal = (Color4)e.NewValue;
                }));

        /// <summary>
        ///         
        /// </summary>
        public static readonly DependencyProperty SpecularColorProperty =
            DependencyProperty.Register("SpecularColor", typeof(Color4), typeof(CustomPhongMaterial), new AffectsRenderPropertyMetadata((Color4)Color.Black,
                (d, e) =>
                {
                    (d as CustomPhongMaterial).SpecularColorInternal = (Color4)e.NewValue;
                }));

        /// <summary>
        ///         
        /// </summary>
        public static readonly DependencyProperty SpecularShininessProperty =
            DependencyProperty.Register("SpecularShininess", typeof(float), typeof(CustomPhongMaterial), new AffectsRenderPropertyMetadata(30f,
                (d, e) =>
                {
                    (d as CustomPhongMaterial).SpecularShininessInternal = (float)e.NewValue;
                }));

        /// <summary>
        ///         
        /// </summary>
        public static readonly DependencyProperty ReflectiveColorProperty =
            DependencyProperty.Register("ReflectiveColor", typeof(Color4), typeof(CustomPhongMaterial), new AffectsRenderPropertyMetadata(new Color4(0.1f, 0.1f, 0.1f, 1.0f),
                (d, e) =>
                {
                    (d as CustomPhongMaterial).ReflectiveColorInternal = (Color4)e.NewValue;
                }));

        /// <summary>
        /// 
        /// </summary>
        public static readonly DependencyProperty DiffuseMapProperty =
            DependencyProperty.Register("DiffuseMap", typeof(Stream), typeof(CustomPhongMaterial), new AffectsRenderPropertyMetadata(null));

        /// <summary>
        /// Supports alpha channel image, such as PNG.
        /// Usage: Load the image file(BMP, PNG, etc) as a stream.
        /// It can be used to replace DiffuseMap, or used as a mask and apply onto diffuse map. 
        /// The color will be cDiffuse*cAlpha.
        /// </summary>
        public static readonly DependencyProperty DiffuseAlphaMapProperty =
            DependencyProperty.Register("DiffuseAlphaMap", typeof(Stream), typeof(CustomPhongMaterial), new AffectsRenderPropertyMetadata(null));

        /// <summary>
        /// 
        /// </summary>
        public static readonly DependencyProperty NormalMapProperty =
            DependencyProperty.Register("NormalMap", typeof(Stream), typeof(CustomPhongMaterial), new AffectsRenderPropertyMetadata(null));

        /// <summary>
        /// 
        /// </summary>
        public static readonly DependencyProperty DisplacementMapProperty =
            DependencyProperty.Register("DisplacementMap", typeof(Stream), typeof(CustomPhongMaterial), new AffectsRenderPropertyMetadata(null));

        /// <summary>
        /// 
        /// </summary>
        public static readonly DependencyProperty SpecularMapProperty =
            DependencyProperty.Register("SpecularMap", typeof(Stream), typeof(CustomPhongMaterial), new AffectsRenderPropertyMetadata(null));

        /// <summary>
        /// 
        /// </summary>
        public static readonly DependencyProperty EmissiveMapProperty =
            DependencyProperty.Register("EmissiveMap", typeof(Stream), typeof(CustomPhongMaterial), new AffectsRenderPropertyMetadata(null));


        /// <summary>
        /// Constructs a Shading Material which correspnds with 
        /// the Phong and BlinnPhong lighting models.
        /// </summary>
        public CustomPhongMaterial() { }

        /// <summary>
        /// Gets or sets a color that represents how the material reflects System.Windows.Media.Media3D.AmbientLight.
        /// For details see: http://msdn.microsoft.com/en-us/library/windows/desktop/bb147175(v=vs.85).aspx
        /// </summary>
        [TypeConverter(typeof(Color4Converter))]
        public Color4 AmbientColor
        {
            get { return (Color4)this.GetValue(AmbientColorProperty); }
            set { this.SetValue(AmbientColorProperty, value); }
        }

        internal Color4 AmbientColorInternal { get; private set; } = (Color4)Color.Gray;
        /// <summary>
        /// Gets or sets the diffuse color for the material.
        /// For details see: http://msdn.microsoft.com/en-us/library/windows/desktop/bb147175(v=vs.85).aspx
        /// </summary>
        [TypeConverter(typeof(Color4Converter))]
        public Color4 DiffuseColor
        {
            get { return (Color4)this.GetValue(DiffuseColorProperty); }
            set { this.SetValue(DiffuseColorProperty, value); }
        }

        internal Color4 DiffuseColorInternal { private set; get; } = (Color4)Color.Gray;
        /// <summary>
        /// Gets or sets the emissive color for the material.
        /// For details see: http://msdn.microsoft.com/en-us/library/windows/desktop/bb147175(v=vs.85).aspx
        /// </summary>
        [TypeConverter(typeof(Color4Converter))]
        public Color4 EmissiveColor
        {
            get { return (Color4)this.GetValue(EmissiveColorProperty); }
            set { this.SetValue(EmissiveColorProperty, value); }
        }

        internal Color4 EmissiveColorInternal { private set; get; } = (Color4)Color.Black;

        /// <summary>
        /// A fake parameter for reflectivity of the environment map
        /// </summary>
        [TypeConverter(typeof(Color4Converter))]
        public Color4 ReflectiveColor
        {
            get { return (Color4)this.GetValue(ReflectiveColorProperty); }
            set { this.SetValue(ReflectiveColorProperty, value); }
        }

        internal Color4 ReflectiveColorInternal { private set; get; } = new Color4(0.1f, 0.1f, 0.1f, 1.0f);
        /// <summary>
        /// Gets or sets the specular color for the material.
        /// For details see: http://msdn.microsoft.com/en-us/library/windows/desktop/bb147175(v=vs.85).aspx
        /// </summary>
        [TypeConverter(typeof(Color4Converter))]
        public Color4 SpecularColor
        {
            get { return (Color4)this.GetValue(SpecularColorProperty); }
            set { this.SetValue(SpecularColorProperty, value); }
        }

        internal Color4 SpecularColorInternal { private set; get; } = (Color4)Color.Black;
        /// <summary>
        /// The power of specular reflections. 
        /// For details see: http://msdn.microsoft.com/en-us/library/windows/desktop/bb147175(v=vs.85).aspx
        /// </summary>
        public float SpecularShininess
        {
            get { return (float)this.GetValue(SpecularShininessProperty); }
            set { this.SetValue(SpecularShininessProperty, value); }
        }

        internal float SpecularShininessInternal { private set; get; } = 30f;
        /// <summary>
        /// System.Windows.Media.Brush to be applied as a System.Windows.Media.Media3D.Material
        /// to a 3-D model.
        /// </summary>
        public Stream DiffuseMap
        {
            get { return (Stream)this.GetValue(DiffuseMapProperty); }
            set { this.SetValue(DiffuseMapProperty, value); }
        }

        public Stream SpecularMap
        {
            get { return (Stream)this.GetValue(SpecularMapProperty); }
            set { this.SetValue(SpecularMapProperty, value); }
        }

        public Stream DiffuseAlphaMap
        {
            get { return (Stream)this.GetValue(DiffuseAlphaMapProperty); }
            set { this.SetValue(DiffuseAlphaMapProperty, value); }
        }

        public Stream EmissiveMap
        {
            get { return (Stream)this.GetValue(EmissiveMapProperty); }
            set { this.SetValue(EmissiveMapProperty, value); }
        }

        /// <summary>
        /// 
        /// </summary>
        public Stream NormalMap
        {
            get { return (Stream)this.GetValue(NormalMapProperty); }
            set { this.SetValue(NormalMapProperty, value); }
        }
        /// <summary>
        /// 
        /// </summary>
        public Stream DisplacementMap
        {
            get { return (Stream)this.GetValue(DisplacementMapProperty); }
            set { this.SetValue(DisplacementMapProperty, value); }
        }

        public CustomPhongMaterial Clone()
        {
            return new CustomPhongMaterial()
            {
                AmbientColor = this.AmbientColor,
                DiffuseColor = this.DiffuseColor,
                DisplacementMap = this.DisplacementMap,
                EmissiveColor = this.EmissiveColor,
                Name = this.Name,
                NormalMap = this.NormalMap,
                ReflectiveColor = this.ReflectiveColor,
                SpecularColor = this.SpecularColor,
                SpecularShininess = this.SpecularShininess,
                DiffuseMap = this.DiffuseMap,
                DiffuseAlphaMap = this.DiffuseAlphaMap,
                SpecularMap = this.SpecularMap,
                EmissiveMap = this.EmissiveMap
            };
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                }

                if(DiffuseMap != null)
                {
                    DiffuseMap.Dispose();
                }

                if(NormalMap != null)
                {
                    NormalMap.Dispose();
                }

                if(DiffuseAlphaMap != null)
                {
                    DiffuseAlphaMap.Dispose();
                }

                if(SpecularMap != null)
                {
                    SpecularMap.Dispose();
                }

                if (EmissiveMap != null)
                {
                    EmissiveMap.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~CustomPhongMaterial() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
