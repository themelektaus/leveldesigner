using UnityEngine;

namespace LevelDesigner.Editor
{
    public class RandomByPosition
    {
        readonly System.Random random;

        public RandomByPosition(Vector2 position, float grid, int z)
        {
            int x = (int) (position.x / grid);
            int y = (int) (position.y / grid);
            random = new System.Random(x * 4000000 + y * 4000 + z);
        }

        public int Next(int min, int max)
        {
            if (min > max)
                min = max;

            return random.Next(min, max + 1);
        }

        public int Next(Vector2Int range)
        {
            return Next(range.x, range.y);
        }

        public Vector3 Next(Vector3 min, Vector3 max)
        {
            return new Vector3(
                Next(min.x, max.x),
                Next(min.y, max.y),
                Next(min.z, max.z)
            );
        }

        float Next()
        {
            return (float) System.Math.Round(random.NextDouble(), 8);
        }

        public float Next(float min, float max)
        {
            if (min > max)
                min = max;

            return min + Next() * (max - min);
        }

        public Vector3 NextCircle(Vector2 range, float y)
        {
            return NextCircle(range.x, range.y, y);
        }

        public Vector3 NextCircle(float min, float max, float y)
        {
            var r = Next(min, max);
            var f = Next() * 360 * Mathf.Deg2Rad;
            return new Vector3(r * Mathf.Sin(f), y, r * Mathf.Cos(f));
        }
    }
}