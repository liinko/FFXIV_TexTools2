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

using System;
using System.Drawing;
using System.Windows;
using System.Windows.Media.Effects;
using System.Windows.Media.Media3D;

namespace FFXIV_TexTools2.Shader
{
    /// <summary>
    /// Shader Effect for color channels
    /// </summary>
    public class ColorChannels : ShaderEffect
    {
        public static readonly DependencyProperty InputProperty = RegisterPixelShaderSamplerProperty("Input", typeof(ColorChannels), 0);

        //public static readonly DependencyProperty ChannelProperty = DependencyProperty.Register("Channels", typeof(Point4D), typeof(ColorChannels),
        //     new UIPropertyMetadata(new Point4D(1.0f, 1.0f, 1.0f, 1.0f), PixelShaderConstantCallback(0)));
        public static readonly DependencyProperty ChannelProperty = DependencyProperty.Register("Channels", typeof(Point4D), typeof(ColorChannels),
     new UIPropertyMetadata(new Point4D(1.0f, 1.0f, 1.0f, 1.0f), PixelShaderConstantCallback(0)));

        private static PixelShader _pixelShader = new PixelShader()
        { UriSource = new Uri("pack://application:,,,/FFXIV TexTools 2;component/Custom/rgbaChannels.cso") };

        public Brush Input
        {
            get { return (Brush)GetValue(InputProperty); }
            set { SetValue(InputProperty, value); }
        }

        public ColorChannels()
        {
            PixelShader = _pixelShader;

            var ei = typeof(PixelShader).GetEvent("_shaderBytecodeChanged", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var fi = typeof(PixelShader).GetField(ei.Name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            fi.SetValue(_pixelShader, null);

            UpdateShaderValue(InputProperty);
            UpdateShaderValue(ChannelProperty);
        }

        public Point4D Channel
        {
            get { return (Point4D)GetValue(ChannelProperty); }
            set { SetValue(ChannelProperty, value); }
        }
    }
}
