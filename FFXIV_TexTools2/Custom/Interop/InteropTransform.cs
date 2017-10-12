using FFXIV_TexTools2.Custom.Interop;

namespace FFXIV_TexTools2.Custom.Interlop
{
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    struct InteropTransform
    {
        public InteropVector4 Translation;
        public InteropVector4 Scale;
        public InteropVector4 Rotation;

        public SharpDX.Matrix ToTransformationMatrix()
        {
            return SharpDX.Matrix.Scaling(Scale.X, Scale.Y, Scale.Z)
                 * SharpDX.Matrix.RotationQuaternion(new SharpDX.Quaternion(Rotation.X, Rotation.Y, Rotation.Z, Rotation.W))
                 * SharpDX.Matrix.Translation(Translation.X, Translation.Y, Translation.Z);
        }
    }
}
