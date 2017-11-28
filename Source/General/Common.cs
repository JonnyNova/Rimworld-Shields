﻿using UnityEngine;
using Verse;

namespace FrontierDevelopments.General
{
    public static class Common
    {
        public static IntVec3 ToIntVec3(Vector2 vector2, int y = 0)
        {
            return new IntVec3((int)vector2.x, y, (int)vector2.y);
        }

        public static Vector2 ToVector2(Vector3 vector3)
        {
            return new Vector2(vector3.x, vector3.z);
        }

        public static Vector2 ToVector2(IntVec3 intVec3)
        {
            return new Vector2(intVec3.x + 0.5f, intVec3.z + 0.5f);
        }

        public static Vector3 ToVector3(Vector2 vector2, float y = 0)
        {
            return new Vector3(vector2.x, y, vector2.y);
        }

        public static Vector3 ToVector3(IntVec3 intVec3)
        {
            return new Vector3(intVec3.x + 0.5f, intVec3.y + 0.5f, intVec3.z + 0.5f);
        }
    }
}