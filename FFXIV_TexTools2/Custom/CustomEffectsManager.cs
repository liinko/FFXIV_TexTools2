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

using HelixToolkit.Wpf.SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;

namespace FFXIV_TexTools2.Shader
{
    public class CustomEffectsManager : DefaultEffectsManager
    {
        public CustomEffectsManager(IRenderTechniquesManager renderTechniquesManager) : base(renderTechniquesManager) { }

        protected override void InitEffects()
        {
            var custom = renderTechniquesManager.RenderTechniques["RenderCustom"];

            RegisterEffect(Properties.Resources._custom, new[] { custom });

            var customInputLayout = new InputLayout(device, GetEffect(custom).GetTechniqueByName("RenderCustom").GetPassByIndex(0).Description.Signature, new[]
            {
                new InputElement("POSITION", 0, Format.R32G32B32A32_Float, InputElement.AppendAligned, 0),
                new InputElement("COLOR",    0, Format.R32G32B32A32_Float, InputElement.AppendAligned, 0),
                new InputElement("TEXCOORD", 0, Format.R32G32_Float,       InputElement.AppendAligned, 0),
                new InputElement("NORMAL",   0, Format.R32G32B32_Float,    InputElement.AppendAligned, 0),
                new InputElement("TANGENT",  0, Format.R32G32B32_Float,    InputElement.AppendAligned, 0),
                new InputElement("BINORMAL", 0, Format.R32G32B32_Float,    InputElement.AppendAligned, 0),
                new InputElement("COLOR",    1, Format.R32G32B32A32_Float, InputElement.AppendAligned, 0),

                //INSTANCING: die 4 texcoords sind die matrix, die mit jedem buffer reinwandern
                new InputElement("TEXCOORD", 1, Format.R32G32B32A32_Float, InputElement.AppendAligned, 1, InputClassification.PerInstanceData, 1),
                new InputElement("TEXCOORD", 2, Format.R32G32B32A32_Float, InputElement.AppendAligned, 1, InputClassification.PerInstanceData, 1),
                new InputElement("TEXCOORD", 3, Format.R32G32B32A32_Float, InputElement.AppendAligned, 1, InputClassification.PerInstanceData, 1),
                new InputElement("TEXCOORD", 4, Format.R32G32B32A32_Float, InputElement.AppendAligned, 1, InputClassification.PerInstanceData, 1),
            });

            RegisterLayout(new[] { custom }, customInputLayout);
        }
    }
}
