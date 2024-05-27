using System.Numerics;

namespace BlakieLibSharp
{
    public struct RectCollider
    {
        public ColliderType colliderType;
        public float x;
        public float y;
        public float width;
        public float height;

        public Vector2 GetCenter()
        {
            return new Vector2(x + width / 2.0f, y + height / 2.0f);
        }

        public Vector2 GetDistanceFrom(RectCollider other)
        {
            return GetDistanceFrom(Vector2.Zero, other, Vector2.Zero);
        }

        public Vector2 GetDistanceFrom(Vector2 ourPos, RectCollider other, Vector2 otherPos)
        {
            float xDistance = -((ourPos.X + x) - (otherPos.X + other.x));
            float yDistance = -((ourPos.Y + y) - (otherPos.Y + other.y));

            Vector2 rtrn = new Vector2();
            if (xDistance >= 0)
                rtrn.X = xDistance + width;
            else
                rtrn.X = xDistance - other.width;

            if (yDistance >= 0)
                rtrn.Y = yDistance + height;
            else
                rtrn.Y = yDistance - other.height;

            return rtrn;
        }

        public bool Overlaps(RectCollider other)
        {
            return Overlaps(Vector2.Zero, other, Vector2.Zero);
        }

        public bool Overlaps(Vector2 ourPos, RectCollider other, Vector2 otherPos)
        {
            if (!ColliderTypeCheck(colliderType, other.colliderType))
                return false;

            if (x + ourPos.X >= other.x + otherPos.X + other.width)
            {
                return false;
            }

            if (x + ourPos.X + width <= other.x + otherPos.X)
            {
                return false;
            }

            if (y + ourPos.Y >= other.y + otherPos.Y + other.height)
            {
                return false;
            }

            if (y + ourPos.Y + height <= other.y + otherPos.Y)
            {
                return false;
            }

            return true;
        }

        public RectCollider Flip()
        {
            return new RectCollider()
            {
                x = -(x + width),
                y = y,
                width = width,
                height = height,
                colliderType = colliderType
            };
        }

        public static bool ColliderTypeCheck(ColliderType checker, ColliderType checking)
        {
            switch (checker)
            {
                default:
                    return false;
                case ColliderType.Push:
                    return checking == ColliderType.Push || checking == ColliderType.Grab;
                case ColliderType.Hurt:
                    return checking == ColliderType.Hit;
                case ColliderType.Hit:
                    return checking == ColliderType.Hurt;
                case ColliderType.Grab:
                    return checking == ColliderType.Push;
            }
        }

        public enum ColliderType : byte
        {
            Push,
            Hurt,
            Hit,
            Snap,
            Connect_T,
            Connect_M,
            Connect_B,
            Grab,
            ObjectLink,
            ColliderTypeCount,
        }
    }
}
