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

        public bool Overlaps(RectCollider other)
        {
            if(!ColliderTypeCheck(colliderType, other.colliderType))
                return false;

            if (x >= other.x + other.width)
            {
                return false;
            }

            if (x + width <= other.x)
            {
                return false;
            }

            if (y >= other.y + other.height)
            {
                return false;
            }

            if (y + height <= other.y)
            {
                return false;
            }

            return true;
        }

        public static bool ColliderTypeCheck(ColliderType checker, ColliderType checking)
        {
            switch(checker)
            {
                default:
                    return false;
                case ColliderType.Hurt:
                    return checking == ColliderType.Hit;
                case ColliderType.Hit:
                    return checking == ColliderType.Hurt;
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
