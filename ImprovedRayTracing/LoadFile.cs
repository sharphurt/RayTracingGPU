﻿using System.IO;

namespace GPURayTracing
{
    public static class LoadFile
    {
        public static string Load(string path) => File.ReadAllText(path);
    }
}