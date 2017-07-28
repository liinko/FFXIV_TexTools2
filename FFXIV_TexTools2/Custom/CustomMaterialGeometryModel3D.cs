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
using System;
using System.ComponentModel;
using System.Windows;

namespace HelixToolkit.Wpf.SharpDX
{
    public abstract class CustomMaterialGeometryModel3D : InstanceGeometryModel3D
    {
        protected InputLayout vertexLayout;
        protected EffectTechnique effectTechnique;
        protected EffectTransformVariables effectTransforms;
        protected EffectMaterialVariables effectMaterial;
        /// <summary>
        /// For subclass override
        /// </summary>
        public abstract IBufferProxy VertexBuffer { get; }
        /// <summary>
        /// For subclass override
        /// </summary>
        public abstract IBufferProxy IndexBuffer { get; }

        protected bool hasShadowMap = false;
        public CustomMaterialGeometryModel3D()
        {
        }

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
        public static readonly DependencyProperty RenderDiffuseMapProperty =
            DependencyProperty.Register("RenderDiffuseMap", typeof(bool), typeof(CustomMaterialGeometryModel3D), new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender));


        public bool RenderColorTable
        {
            get { return (bool)this.GetValue(RenderColorTableProperty); }
            set { this.SetValue(RenderColorTableProperty, value); }
        }

        /// <summary>
        /// 
        /// </summary>
        public static readonly DependencyProperty RenderColorTableProperty =
            DependencyProperty.Register("RenderColorTable", typeof(bool), typeof(CustomMaterialGeometryModel3D), new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender));


        public bool RenderMaskMap
        {
            get { return (bool)this.GetValue(RenderMaskMapProperty); }
            set { this.SetValue(RenderMaskMapProperty, value); }
        }

        /// <summary>
        /// 
        /// </summary>
        public static readonly DependencyProperty RenderMaskMapProperty =
            DependencyProperty.Register("RenderMaskMap", typeof(bool), typeof(CustomMaterialGeometryModel3D), new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender));


        public bool RenderSpecularMap
        {
            get { return (bool)this.GetValue(RenderSpecularMapProperty); }
            set { this.SetValue(RenderSpecularMapProperty, value); }
        }

        /// <summary>
        /// 
        /// </summary>
        public static readonly DependencyProperty RenderSpecularMapProperty =
            DependencyProperty.Register("RenderSpecularMap", typeof(bool), typeof(CustomMaterialGeometryModel3D), new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender));
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
        public static readonly DependencyProperty RenderAlphaDiffuseMapProperty =
            DependencyProperty.Register("RenderAlphaDiffuseMap", typeof(bool), typeof(CustomMaterialGeometryModel3D), new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// 
        /// </summary>
        public bool RenderAlphaDiffuseMap
        {
            get { return (bool)this.GetValue(RenderAlphaDiffuseMapProperty); }
            set { this.SetValue(RenderAlphaDiffuseMapProperty, value); }
        }
        /// <summary>
        /// 
        /// </summary>
        public static readonly DependencyProperty RenderNormalMapProperty =
            DependencyProperty.Register("RenderNormalMap", typeof(bool), typeof(CustomMaterialGeometryModel3D), new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender));

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
        public static readonly DependencyProperty RenderDisplacementMapProperty =
            DependencyProperty.Register("RenderDisplacementMap", typeof(bool), typeof(CustomMaterialGeometryModel3D), new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender));

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
        public static readonly DependencyProperty MaterialProperty =
            DependencyProperty.Register("Material", typeof(Material), typeof(CustomMaterialGeometryModel3D), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, MaterialChanged));

        /// <summary>
        /// 
        /// </summary>
        protected static void MaterialChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is CustomPhongMaterial)
            {
                var model = ((CustomMaterialGeometryModel3D)d);
                if (model.IsAttached)
                {
                    var host = model.renderHost;
                    model.Detach();
                    model.Attach(host);
                }
            }
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

        /// <summary>
        /// 
        /// </summary>
        public static readonly DependencyProperty TextureCoodScaleProperty =
            DependencyProperty.Register("TextureCoodScale", typeof(Vector2), typeof(CustomMaterialGeometryModel3D), new FrameworkPropertyMetadata(new Vector2(1, 1), FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// 
        /// </summary>
        protected virtual void AttachMaterial()
        {
            var phongMaterial = Material as CustomPhongMaterial;
            if (phongMaterial != null)
            {
                this.effectMaterial = new EffectMaterialVariables(this.effect, phongMaterial);
                this.effectMaterial.CreateTextureViews(Device, this);
            }
        }

        public class EffectMaterialVariables : System.IDisposable
        {
            private CustomPhongMaterial material;
            private ShaderResourceView texDiffuseAlphaMapView;
            private ShaderResourceView texDiffuseMapView;
            private ShaderResourceView texNormalMapView;
            private ShaderResourceView texDisplacementMapView;
            private ShaderResourceView texColorTableView;
            private ShaderResourceView texMaskMapView;
            private ShaderResourceView texSpecularMapView;


            private EffectVectorVariable vMaterialAmbientVariable, vMaterialDiffuseVariable, vMaterialEmissiveVariable, vMaterialSpecularVariable, vMaterialReflectVariable;
            private EffectScalarVariable sMaterialShininessVariable;
            private EffectScalarVariable bHasDiffuseMapVariable, bHasNormalMapVariable, bHasDisplacementMapVariable, bHasDiffuseAlphaMapVariable;
            public EffectScalarVariable bHasColorTableVariable, bHasMaskMapVariable, bHasSpecularMapVariable;
            private EffectShaderResourceVariable texDiffuseMapVariable, texNormalMapVariable, texDisplacementMapVariable, texShadowMapVariable, texDiffuseAlphaMapVariable, texColorTableVariable, texMaskMapVariable, texSpecularMapVariable;
            public EffectScalarVariable bHasShadowMapVariable;
            public EffectMaterialVariables(Effect effect, CustomPhongMaterial material)
            {
                this.material = material;

                this.vMaterialAmbientVariable   = effect.GetVariableByName("vMaterialAmbient").AsVector();
                this.vMaterialDiffuseVariable   = effect.GetVariableByName("vMaterialDiffuse").AsVector();
                this.vMaterialEmissiveVariable  = effect.GetVariableByName("vMaterialEmissive").AsVector();
                this.vMaterialSpecularVariable  = effect.GetVariableByName("vMaterialSpecular").AsVector();
                this.vMaterialReflectVariable   = effect.GetVariableByName("vMaterialReflect").AsVector();

                this.sMaterialShininessVariable = effect.GetVariableByName("sMaterialShininess").AsScalar();
                this.bHasDiffuseMapVariable     = effect.GetVariableByName("bHasDiffuseMap").AsScalar();
                this.bHasDiffuseAlphaMapVariable = effect.GetVariableByName("bHasAlphaMap").AsScalar();
                this.bHasNormalMapVariable      = effect.GetVariableByName("bHasNormalMap").AsScalar();
                this.bHasDisplacementMapVariable = effect.GetVariableByName("bHasDisplacementMap").AsScalar();
                this.bHasShadowMapVariable      = effect.GetVariableByName("bHasShadowMap").AsScalar();
                this.bHasColorTableVariable      = effect.GetVariableByName("bHasColorTable").AsScalar();
                this.bHasMaskMapVariable        = effect.GetVariableByName("bHasMaskMap").AsScalar();
                this.bHasSpecularMapVariable    = effect.GetVariableByName("bHasSpecularMap").AsScalar();

                this.texDiffuseMapVariable      = effect.GetVariableByName("texDiffuseMap").AsShaderResource();
                this.texNormalMapVariable       = effect.GetVariableByName("texNormalMap").AsShaderResource();
                this.texDisplacementMapVariable = effect.GetVariableByName("texDisplacementMap").AsShaderResource();
                this.texShadowMapVariable       = effect.GetVariableByName("texShadowMap").AsShaderResource();
                this.texDiffuseAlphaMapVariable = effect.GetVariableByName("texAlphaMap").AsShaderResource();
                this.texColorTableVariable      = effect.GetVariableByName("texColorTable").AsShaderResource();
                this.texMaskMapVariable         = effect.GetVariableByName("texMaskMap").AsShaderResource();
                this.texSpecularMapVariable     = effect.GetVariableByName("texSpecularMap").AsShaderResource();
            }

            public void CreateTextureViews(Device device, CustomMaterialGeometryModel3D model)
            {
                if (material != null)
                {
                    /// --- has texture
                    if (material.DiffuseMap != null && model.RenderDiffuseMap)
                    {
                        Disposer.RemoveAndDispose(ref this.texDiffuseMapView);
                        this.texDiffuseMapView = TextureLoader.FromMemoryAsShaderResourceView(device, material.DiffuseMap);
                    }

                    if (material.DiffuseAlphaMap != null)
                    {
                        Disposer.RemoveAndDispose(ref this.texDiffuseAlphaMapView);
                        this.texDiffuseAlphaMapView = TextureLoader.FromMemoryAsShaderResourceView(device, material.DiffuseAlphaMap);
                    }

                    // --- has bumpmap
                    if (material.NormalMap != null && model.RenderNormalMap)
                    {
                        var geometry = model.geometryInternal as MeshGeometry3D;
                        if (geometry != null)
                        {
                            if (geometry.Tangents == null)
                            {
                                //System.Windows.MessageBox.Show(string.Format("No Tangent-Space found. NormalMap will be omitted."), "Warrning", MessageBoxButton.OK);
                                material.NormalMap = null;
                            }
                            else
                            {
                                Disposer.RemoveAndDispose(ref this.texNormalMapView);
                                this.texNormalMapView = TextureLoader.FromMemoryAsShaderResourceView(device, material.NormalMap);
                            }
                        }
                    }

                    // --- has displacement map
                    if (material.DisplacementMap != null && model.RenderDisplacementMap)
                    {
                        Disposer.RemoveAndDispose(ref this.texDisplacementMapView);
                        this.texDisplacementMapView = TextureLoader.FromMemoryAsShaderResourceView(device, material.DisplacementMap);
                    }

                    // --- has color table
                    if(material.ColorTable != null && model.RenderColorTable)
                    {
                        Disposer.RemoveAndDispose(ref this.texColorTableView);
                        this.texColorTableView = TextureLoader.FromMemoryAsShaderResourceView(device, material.ColorTable);
                    }

                    // --- has mask map
                    if (material.MaskMap != null && model.RenderMaskMap)
                    {
                        Disposer.RemoveAndDispose(ref this.texMaskMapView);
                        this.texMaskMapView = TextureLoader.FromMemoryAsShaderResourceView(device, material.MaskMap);
                    }

                    // --- has specular map
                    if (material.SpecularMap != null && model.RenderSpecularMap)
                    {
                        Disposer.RemoveAndDispose(ref this.texSpecularMapView);
                        this.texSpecularMapView = TextureLoader.FromMemoryAsShaderResourceView(device, material.SpecularMap);
                    }
                }
            }

            public bool AttachMaterial()
            {
                // --- has samples              
                this.bHasDiffuseMapVariable.Set(this.texDiffuseMapView != null);
                this.bHasDiffuseAlphaMapVariable.Set(this.texDiffuseAlphaMapView != null);
                this.bHasNormalMapVariable.Set(this.texNormalMapView != null);
                this.bHasDisplacementMapVariable.Set(this.texDisplacementMapView != null);
                this.bHasColorTableVariable.Set(this.texColorTableView != null);
                this.bHasMaskMapVariable.Set(this.texMaskMapView != null);
                this.bHasSpecularMapVariable.Set(this.texSpecularMapView != null);

                if (material != null)
                {
                    this.vMaterialDiffuseVariable.Set(material.DiffuseColorInternal);
                    this.vMaterialAmbientVariable.Set(material.AmbientColorInternal);
                    this.vMaterialEmissiveVariable.Set(material.EmissiveColorInternal);
                    this.vMaterialSpecularVariable.Set(material.SpecularColorInternal);
                    this.vMaterialReflectVariable.Set(material.ReflectiveColorInternal);
                    this.sMaterialShininessVariable.Set(material.SpecularShininessInternal);

                    this.texDiffuseMapVariable.SetResource(this.texDiffuseMapView);
                    this.texNormalMapVariable.SetResource(this.texNormalMapView);
                    this.texDiffuseAlphaMapVariable.SetResource(this.texDiffuseAlphaMapView);
                    this.texDisplacementMapVariable.SetResource(this.texDisplacementMapView);
                    this.texColorTableVariable.SetResource(this.texColorTableView);
                    this.texMaskMapVariable.SetResource(this.texMaskMapView);
                    this.texSpecularMapVariable.SetResource(this.texSpecularMapView);

                    return true;
                }
                else
                {
                    return false;
                }
            }

            public void Dispose()
            {
                modelDisposer();
            }

            private void modelDisposer()
            {
                BackgroundWorker modelDispose = new BackgroundWorker()
                {
                    WorkerReportsProgress = true,
                };
                modelDispose.DoWork += new DoWorkEventHandler(ModelDispose_Work);
                modelDispose.ProgressChanged += new ProgressChangedEventHandler(ModelDispose_ProgressChanged);
                modelDispose.RunWorkerCompleted += new RunWorkerCompletedEventHandler(ModelDispose_RunWorkerCompleted);
                modelDispose.RunWorkerAsync();
            }

            private void ModelDispose_Work(object sender, DoWorkEventArgs e)
            {
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
                Disposer.RemoveAndDispose(ref this.bHasColorTableVariable);
                Disposer.RemoveAndDispose(ref this.bHasMaskMapVariable);
                Disposer.RemoveAndDispose(ref this.bHasSpecularMapVariable);

                Disposer.RemoveAndDispose(ref this.texDiffuseMapVariable);
                Disposer.RemoveAndDispose(ref this.texNormalMapVariable);
                Disposer.RemoveAndDispose(ref this.texDisplacementMapVariable);
                Disposer.RemoveAndDispose(ref this.texShadowMapVariable);
                Disposer.RemoveAndDispose(ref this.texDiffuseAlphaMapVariable);
                Disposer.RemoveAndDispose(ref this.texColorTableVariable);
                Disposer.RemoveAndDispose(ref this.texMaskMapVariable);
                Disposer.RemoveAndDispose(ref this.texSpecularMapVariable);

                Disposer.RemoveAndDispose(ref this.texDiffuseMapView);
                Disposer.RemoveAndDispose(ref this.texNormalMapView);
                Disposer.RemoveAndDispose(ref this.texDisplacementMapView);
                Disposer.RemoveAndDispose(ref this.texDiffuseAlphaMapView);
                Disposer.RemoveAndDispose(ref this.texColorTableView);
                Disposer.RemoveAndDispose(ref this.texMaskMapView);
                Disposer.RemoveAndDispose(ref this.texSpecularMapView);
            }

            private void ModelDispose_ProgressChanged(object sender, ProgressChangedEventArgs e)
            {
                throw new NotImplementedException();
            }

            private void ModelDispose_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
            {
                //throw new NotImplementedException();
            }
        }

       

        /// <summary>
        /// 
        /// </summary>
        protected override void OnDetach()
        {
            Disposer.RemoveAndDispose(ref this.effectMaterial);
            Disposer.RemoveAndDispose(ref this.effectTransforms);

            this.effectTechnique = null;
            this.vertexLayout = null;

            base.OnDetach();
        }
    }
}
