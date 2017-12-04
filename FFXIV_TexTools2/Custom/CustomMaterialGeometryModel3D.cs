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

using HelixToolkit.Wpf.SharpDX.Utilities;
using SharpDX;
using SharpDX.Direct3D11;
using System.ComponentModel;
using System.Windows;

namespace HelixToolkit.Wpf.SharpDX
{
    public abstract class CustomMaterialGeometryModel3D : InstanceGeometryModel3D
    {
        #region Dependency Properties
        /// <summary>
        /// 
        /// </summary>
        public static readonly DependencyProperty RenderDiffuseMapProperty =
            DependencyProperty.Register("RenderDiffuseMap", typeof(bool), typeof(CustomMaterialGeometryModel3D), new AffectsRenderPropertyMetadata(true,
                (d, e) =>
                {
                    var model = d as CustomMaterialGeometryModel3D;
                    if (model.effectMaterial != null)
                    {
                        model.effectMaterial.RenderDiffuseMap = (bool)e.NewValue;
                    }
                }));
        /// <summary>
        /// 
        /// </summary>
        public static readonly DependencyProperty RenderDiffuseAlphaMapProperty =
            DependencyProperty.Register("RenderDiffuseAlphaMap", typeof(bool), typeof(CustomMaterialGeometryModel3D), new AffectsRenderPropertyMetadata(true,
                (d, e) =>
                {
                    var model = d as CustomMaterialGeometryModel3D;
                    if (model.effectMaterial != null)
                    {
                        model.effectMaterial.RenderDiffuseAlphaMap = (bool)e.NewValue;
                    }
                }));
        /// <summary>
        /// 
        /// </summary>
        public static readonly DependencyProperty RenderNormalMapProperty =
            DependencyProperty.Register("RenderNormalMap", typeof(bool), typeof(CustomMaterialGeometryModel3D), new AffectsRenderPropertyMetadata(true,
                (d, e) =>
                {
                    var model = d as CustomMaterialGeometryModel3D;
                    if (model.effectMaterial != null)
                    {
                        model.effectMaterial.RenderNormalMap = (bool)e.NewValue;
                    }
                }));
        /// <summary>
        /// 
        /// </summary>
        public static readonly DependencyProperty RenderDisplacementMapProperty =
            DependencyProperty.Register("RenderDisplacementMap", typeof(bool), typeof(CustomMaterialGeometryModel3D), new AffectsRenderPropertyMetadata(true,
                (d, e) =>
                {
                    var model = d as CustomMaterialGeometryModel3D;
                    if (model.effectMaterial != null)
                    {
                        model.effectMaterial.RenderDisplacementMap = (bool)e.NewValue;
                    }
                }));
        /// <summary>
        /// 
        /// </summary>
        public static readonly DependencyProperty RenderSpecularMapProperty =
            DependencyProperty.Register("RenderSpecularMap", typeof(bool), typeof(CustomMaterialGeometryModel3D), new AffectsRenderPropertyMetadata(true,
                (d, e) =>
                {
                    var model = d as CustomMaterialGeometryModel3D;
                    if (model.effectMaterial != null)
                    {
                        model.effectMaterial.RenderSpecularMap = (bool)e.NewValue;
                    }
                }));
        /// <summary>
        /// 
        /// </summary>
        public static readonly DependencyProperty RenderEmissiveMapProperty =
            DependencyProperty.Register("RenderEmissiveMap", typeof(bool), typeof(CustomMaterialGeometryModel3D), new AffectsRenderPropertyMetadata(true,
                (d, e) =>
                {
                    var model = d as CustomMaterialGeometryModel3D;
                    if (model.effectMaterial != null)
                    {
                        model.effectMaterial.RenderEmissiveMap = (bool)e.NewValue;
                    }
                }));
        /// <summary>
        /// 
        /// </summary>
        public static readonly DependencyProperty MaterialProperty =
            DependencyProperty.Register("Material", typeof(Material), typeof(CustomMaterialGeometryModel3D), new AffectsRenderPropertyMetadata(null, MaterialChanged));


        /// <summary>
        /// 
        /// </summary>
        public static readonly DependencyProperty TextureCoodScaleProperty =
            DependencyProperty.Register("TextureCoodScale", typeof(Vector2), typeof(CustomMaterialGeometryModel3D), new AffectsRenderPropertyMetadata(new Vector2(1, 1)));


        /// <summary>
        /// 
        /// </summary>
        public bool RenderDiffuseMap
        {
            get { return (bool)this.GetValue(RenderDiffuseMapProperty); }
            set { this.SetValue(RenderDiffuseMapProperty, value); }
        }

        /// <summary>
        /// 
        /// </summary>
        public bool RenderNormalMap
        {
            get { return (bool)this.GetValue(RenderNormalMapProperty); }
            set { this.SetValue(RenderNormalMapProperty, value); }
        }

        /// <summary>
        /// 
        /// </summary>
        public bool RenderDiffuseAlphaMap
        {
            get { return (bool)this.GetValue(RenderDiffuseAlphaMapProperty); }
            set { this.SetValue(RenderDiffuseAlphaMapProperty, value); }
        }


        /// <summary>
        /// 
        /// </summary>
        public bool RenderDisplacementMap
        {
            get { return (bool)this.GetValue(RenderDisplacementMapProperty); }
            set { this.SetValue(RenderDisplacementMapProperty, value); }
        }

        /// <summary>
        /// 
        /// </summary>
        public bool RenderSpecularMap
        {
            get { return (bool)this.GetValue(RenderSpecularMapProperty); }
            set { this.SetValue(RenderSpecularMapProperty, value); }
        }

        /// <summary>
        /// 
        /// </summary>
        public bool RenderEmissiveMap
        {
            get { return (bool)this.GetValue(RenderEmissiveMapProperty); }
            set { this.SetValue(RenderEmissiveMapProperty, value); }
        }

        /// <summary>
        /// 
        /// </summary>
        public Material Material
        {
            get { return (Material)this.GetValue(MaterialProperty); }
            set { this.SetValue(MaterialProperty, value); }
        }
        /// <summary>
        /// 
        /// </summary>
        [TypeConverter(typeof(Vector2Converter))]
        public Vector2 TextureCoodScale
        {
            get { return (Vector2)this.GetValue(TextureCoodScaleProperty); }
            set { this.SetValue(TextureCoodScaleProperty, value); }
        }
        #endregion

        #region Static Methods
        /// <summary>
        /// 
        /// </summary>
        protected static void MaterialChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is CustomPhongMaterial)
            {
                var model = ((CustomMaterialGeometryModel3D)d);
                model.materialInternal = e.NewValue as CustomPhongMaterial;
                if (model.renderHost != null)
                {
                    if (model.IsAttached)
                    {
                        model.AttachMaterial();
                        model.InvalidateRender();
                    }
                    else
                    {
                        var host = model.renderHost;
                        model.Detach();
                        model.Attach(host);
                    }
                }
            }
        }
        #endregion

        #region Variables
        protected bool hasShadowMap = false;

        protected EffectMaterialVariables effectMaterial;
        #endregion
        #region Properties
        protected CustomPhongMaterial materialInternal { private set; get; }
        /// <summary>
        /// For subclass override
        /// </summary>
        public abstract IBufferProxy VertexBuffer { get; }
        /// <summary>
        /// For subclass override
        /// </summary>
        public abstract IBufferProxy IndexBuffer { get; }

        #endregion

        /// <summary>
        /// 
        /// </summary>
        protected virtual void AttachMaterial()
        {
            Disposer.RemoveAndDispose(ref this.effectMaterial);
            if (materialInternal != null)
            {
                this.effectMaterial = new EffectMaterialVariables(this.effect, materialInternal);
                this.effectMaterial.CreateTextureViews(Device, this);
                this.effectMaterial.RenderDiffuseMap = this.RenderDiffuseMap;
                this.effectMaterial.RenderDiffuseAlphaMap = this.RenderDiffuseAlphaMap;
                this.effectMaterial.RenderNormalMap = this.RenderNormalMap;
                this.effectMaterial.RenderDisplacementMap = this.RenderDisplacementMap;
                this.effectMaterial.OnInvalidateRenderer += (s, e) => { InvalidateRender(); };
            }
        }

        protected override bool OnAttach(IRenderHost host)
        {
            // --- attach
            if (!base.OnAttach(host))
            {
                return false;
            }
            // --- material 
            this.AttachMaterial();
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        protected override void OnDetach()
        {
            Disposer.RemoveAndDispose(ref this.effectMaterial);
            base.OnDetach();
        }


        public class EffectMaterialVariables : System.IDisposable
        {
            public event System.EventHandler OnInvalidateRenderer;
            private readonly CustomPhongMaterial material;
            private Device device;
            private ShaderResourceView texDiffuseAlphaMapView;
            private ShaderResourceView texDiffuseMapView;
            private ShaderResourceView texNormalMapView;
            private ShaderResourceView texDisplacementMapView;
            private ShaderResourceView texSpecularMapView;
            private ShaderResourceView texEmissiveMapView;
            private EffectVectorVariable vMaterialAmbientVariable, vMaterialDiffuseVariable, vMaterialEmissiveVariable, vMaterialSpecularVariable, vMaterialReflectVariable;
            private EffectScalarVariable sMaterialShininessVariable;
            private EffectScalarVariable bHasDiffuseMapVariable, bHasNormalMapVariable, bHasDisplacementMapVariable, bHasDiffuseAlphaMapVariable, bHasSpecularMapVariable, bHasEmissiveMapVariable;
            private EffectShaderResourceVariable texDiffuseMapVariable, texNormalMapVariable, texDisplacementMapVariable, texShadowMapVariable, texDiffuseAlphaMapVariable, texSpecularMapVariable, texEmissiveMapVariable;
            public EffectScalarVariable bHasShadowMapVariable;

            public bool RenderDiffuseMap { set; get; } = true;
            public bool RenderDiffuseAlphaMap { set; get; } = true;
            public bool RenderNormalMap { set; get; } = true;
            public bool RenderDisplacementMap { set; get; } = true;
            public bool RenderSpecularMap { set; get; } = true;
            public bool RenderEmissiveMap { set; get; } = true;

            public EffectMaterialVariables(Effect effect, CustomPhongMaterial material)
            {
                this.material = material;
                this.material.OnMaterialPropertyChanged += Material_OnMaterialPropertyChanged;
                this.vMaterialAmbientVariable = effect.GetVariableByName("vMaterialAmbient").AsVector();
                this.vMaterialDiffuseVariable = effect.GetVariableByName("vMaterialDiffuse").AsVector();
                this.vMaterialEmissiveVariable = effect.GetVariableByName("vMaterialEmissive").AsVector();
                this.vMaterialSpecularVariable = effect.GetVariableByName("vMaterialSpecular").AsVector();
                this.vMaterialReflectVariable = effect.GetVariableByName("vMaterialReflect").AsVector();
                this.sMaterialShininessVariable = effect.GetVariableByName("sMaterialShininess").AsScalar();
                this.bHasDiffuseMapVariable = effect.GetVariableByName("bHasDiffuseMap").AsScalar();
                this.bHasDiffuseAlphaMapVariable = effect.GetVariableByName("bHasAlphaMap").AsScalar();
                this.bHasNormalMapVariable = effect.GetVariableByName("bHasNormalMap").AsScalar();
                this.bHasDisplacementMapVariable = effect.GetVariableByName("bHasDisplacementMap").AsScalar();
                this.bHasShadowMapVariable = effect.GetVariableByName("bHasShadowMap").AsScalar();
                this.bHasSpecularMapVariable = effect.GetVariableByName("bHasSpecularMap").AsScalar();
                this.bHasEmissiveMapVariable = effect.GetVariableByName("bHasEmissiveMap").AsScalar();
                this.texDiffuseMapVariable = effect.GetVariableByName("texDiffuseMap").AsShaderResource();
                this.texNormalMapVariable = effect.GetVariableByName("texNormalMap").AsShaderResource();
                this.texDisplacementMapVariable = effect.GetVariableByName("texDisplacementMap").AsShaderResource();
                this.texShadowMapVariable = effect.GetVariableByName("texShadowMap").AsShaderResource();
                this.texDiffuseAlphaMapVariable = effect.GetVariableByName("texAlphaMap").AsShaderResource();
                this.texSpecularMapVariable = effect.GetVariableByName("texSpecularMap").AsShaderResource();
                this.texEmissiveMapVariable = effect.GetVariableByName("texEmissiveMap").AsShaderResource();

            }

            private void Material_OnMaterialPropertyChanged(object sender, MaterialPropertyChanged e)
            {
                if (e.PropertyName.Equals(nameof(material.DiffuseMap)))
                {
                    CreateTextureView(material.DiffuseMap, ref this.texDiffuseMapView);
                }
                else if (e.PropertyName.Equals(nameof(material.NormalMap)))
                {
                    CreateTextureView(material.NormalMap, ref this.texNormalMapView);
                }
                else if (e.PropertyName.Equals(nameof(material.DisplacementMap)))
                {
                    CreateTextureView(material.DisplacementMap, ref this.texDisplacementMapView);
                }
                else if (e.PropertyName.Equals(nameof(material.DiffuseAlphaMap)))
                {
                    CreateTextureView(material.DiffuseAlphaMap, ref this.texDiffuseAlphaMapView);
                }
                else if (e.PropertyName.Equals(nameof(material.SpecularMap)))
                {
                    CreateTextureView(material.SpecularMap, ref this.texSpecularMapView);
                }
                else if (e.PropertyName.Equals(nameof(material.EmissiveMap)))
                {
                    CreateTextureView(material.EmissiveMap, ref this.texEmissiveMapView);
                }
                OnInvalidateRenderer?.Invoke(this, null);
            }

            private void CreateTextureView(System.IO.Stream stream, ref ShaderResourceView textureView)
            {
                Disposer.RemoveAndDispose(ref textureView);
                if (stream != null && device != null)
                {
                    textureView = TextureLoader.FromMemoryAsShaderResourceView(device, stream);
                }
            }

            public void CreateTextureViews(Device device, CustomMaterialGeometryModel3D model)
            {
                this.device = device;
                if (material != null)
                {
                    CreateTextureView(material.DiffuseMap, ref this.texDiffuseMapView);
                    CreateTextureView(material.NormalMap, ref this.texNormalMapView);
                    CreateTextureView(material.DisplacementMap, ref this.texDisplacementMapView);
                    CreateTextureView(material.DiffuseAlphaMap, ref this.texDiffuseAlphaMapView);
                    CreateTextureView(material.SpecularMap, ref this.texSpecularMapView);
                    CreateTextureView(material.EmissiveMap, ref this.texEmissiveMapView);
                }
                else
                {
                    Disposer.RemoveAndDispose(ref this.texDiffuseMapView);
                    Disposer.RemoveAndDispose(ref this.texNormalMapView);
                    Disposer.RemoveAndDispose(ref this.texDisplacementMapView);
                    Disposer.RemoveAndDispose(ref this.texDiffuseAlphaMapView);
                    Disposer.RemoveAndDispose(ref this.texSpecularMapView);
                    Disposer.RemoveAndDispose(ref this.texEmissiveMapView);
                }
            }

            public bool AttachMaterial(MeshGeometry3D model)
            {
                if (material == null || model == null)
                {
                    return false;
                }
                this.vMaterialDiffuseVariable.Set(material.DiffuseColorInternal);
                this.vMaterialAmbientVariable.Set(material.AmbientColorInternal);
                this.vMaterialEmissiveVariable.Set(material.EmissiveColorInternal);
                this.vMaterialSpecularVariable.Set(material.SpecularColorInternal);
                this.vMaterialReflectVariable.Set(material.ReflectiveColorInternal);
                this.sMaterialShininessVariable.Set(material.SpecularShininessInternal);

                // --- has samples              
                bool hasDiffuseMap = RenderDiffuseMap && this.texDiffuseMapView != null;
                this.bHasDiffuseMapVariable.Set(hasDiffuseMap);
                if (hasDiffuseMap)
                { this.texDiffuseMapVariable.SetResource(this.texDiffuseMapView); }

                bool hasDiffuseAlphaMap = RenderDiffuseAlphaMap && this.texDiffuseAlphaMapView != null;
                this.bHasDiffuseAlphaMapVariable.Set(hasDiffuseAlphaMap);
                if (hasDiffuseAlphaMap)
                {
                    this.texDiffuseAlphaMapVariable.SetResource(this.texDiffuseAlphaMapView);
                }

                bool hasNormalMap = RenderNormalMap && this.texNormalMapView != null && model.Tangents != null;
                this.bHasNormalMapVariable.Set(hasNormalMap);
                if (hasNormalMap)
                {
                    this.texNormalMapVariable.SetResource(this.texNormalMapView);
                }

                bool hasDisplacementMap = RenderDisplacementMap && this.texDisplacementMapView != null && model.BiTangents != null;
                this.bHasDisplacementMapVariable.Set(hasDisplacementMap);
                if (hasDisplacementMap)
                {
                    this.texDisplacementMapVariable.SetResource(this.texDisplacementMapView);
                }

                bool hasSpecularMap = RenderSpecularMap && this.texSpecularMapView != null;
                this.bHasSpecularMapVariable.Set(hasSpecularMap);
                if (hasSpecularMap)
                {
                    this.texSpecularMapVariable.SetResource(this.texSpecularMapView);
                }

                bool hasEmissiveMap = RenderEmissiveMap && this.texEmissiveMapView != null;
                this.bHasEmissiveMapVariable.Set(hasEmissiveMap);
                if (hasEmissiveMap)
                {
                    this.texEmissiveMapVariable.SetResource(this.texEmissiveMapView);
                }

                return true;
            }

            public void Dispose()
            {
                this.material.OnMaterialPropertyChanged -= Material_OnMaterialPropertyChanged;
                OnInvalidateRenderer = null;
                Disposer.RemoveAndDispose(ref this.vMaterialAmbientVariable);
                Disposer.RemoveAndDispose(ref this.vMaterialDiffuseVariable);
                Disposer.RemoveAndDispose(ref this.vMaterialEmissiveVariable);
                Disposer.RemoveAndDispose(ref this.vMaterialSpecularVariable);
                Disposer.RemoveAndDispose(ref this.sMaterialShininessVariable);
                Disposer.RemoveAndDispose(ref this.vMaterialReflectVariable);
                Disposer.RemoveAndDispose(ref this.bHasDiffuseMapVariable);
                Disposer.RemoveAndDispose(ref this.bHasNormalMapVariable);
                Disposer.RemoveAndDispose(ref this.bHasDisplacementMapVariable);
                Disposer.RemoveAndDispose(ref this.bHasShadowMapVariable);
                Disposer.RemoveAndDispose(ref this.bHasDiffuseAlphaMapVariable);
                Disposer.RemoveAndDispose(ref this.bHasSpecularMapVariable);
                Disposer.RemoveAndDispose(ref this.bHasEmissiveMapVariable);
                Disposer.RemoveAndDispose(ref this.texDiffuseMapVariable);
                Disposer.RemoveAndDispose(ref this.texNormalMapVariable);
                Disposer.RemoveAndDispose(ref this.texDisplacementMapVariable);
                Disposer.RemoveAndDispose(ref this.texShadowMapVariable);
                Disposer.RemoveAndDispose(ref this.texDiffuseAlphaMapVariable);
                Disposer.RemoveAndDispose(ref this.texSpecularMapVariable);
                Disposer.RemoveAndDispose(ref this.texEmissiveMapVariable);
                Disposer.RemoveAndDispose(ref this.texDiffuseMapView);
                Disposer.RemoveAndDispose(ref this.texNormalMapView);
                Disposer.RemoveAndDispose(ref this.texSpecularMapView);
                Disposer.RemoveAndDispose(ref this.texEmissiveMapView);
                Disposer.RemoveAndDispose(ref this.texDisplacementMapView);
                Disposer.RemoveAndDispose(ref this.texDiffuseAlphaMapView);

            }
        }

    }
}