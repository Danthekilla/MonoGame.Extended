﻿namespace MonoGame.Extended.Particles.Profiles
{
    public class BoxUniformProfile : Profile
    {
        public float Width { get; set; }
        public float Height { get; set; }

        public override void GetOffsetAndHeading(out Vector offset, out Axis heading)
        {
            var value = FastRand.NextInteger((int)(2 * Width + 2 * Height));

            if (value < Width) // Top
                offset = new Vector(FastRand.NextSingle(Width * -0.5f, Width * 0.5f), Height * -0.5f);
            else if (value < 2 * Width) // Bottom
                offset = new Vector(FastRand.NextSingle(Width * -0.5f, Width * 0.5f), Height * 0.5f);
            else if (value < 2 * Width + Height) // Left
                offset = new Vector(Width * -0.5f, FastRand.NextSingle(Height * -0.5f, Height * 0.5f));
            else // Right
                offset = new Vector(Width * 0.5f, FastRand.NextSingle(Height * -0.5f, Height * 0.5f));

            FastRand.NextUnitVector(out heading);
        }
    }
}