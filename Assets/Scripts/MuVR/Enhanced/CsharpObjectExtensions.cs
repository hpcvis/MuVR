﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;

namespace MuVR.Enhanced {
	public static class CsharpObjectExtensions {
		
		private static Dictionary<Type, Delegate> CachedIl { get; } = new();
		
		// Function that clones an object into another object using IL (used to copy a base class into a derived class)
		// From: https://stackoverflow.com/questions/14613919/copying-the-contents-of-a-base-class-from-a-derived-class
		public static void CloneObjectWithIL<T>(T source, T destination) {
			//See http://lindexi.oschina.io/lindexi/post/C-%E4%BD%BF%E7%94%A8Emit%E6%B7%B1%E5%85%8B%E9%9A%86/
			if (CachedIl.ContainsKey(typeof(T))) {
				((Action<T, T>)CachedIl[typeof(T)])(source, destination);
				return;
			}

			var dynamicMethod = new DynamicMethod("Clone", null, new[] { typeof(T), typeof(T) });
			var generator = dynamicMethod.GetILGenerator();

			foreach (var temp in typeof(T).GetProperties().Where(temp => temp.CanRead && temp.CanWrite)) {
				if (temp.GetAccessors(true)[0].IsStatic) continue;

				generator.Emit(OpCodes.Ldarg_1); // destination
				generator.Emit(OpCodes.Ldarg_0); // s
				generator.Emit(OpCodes.Callvirt, temp.GetMethod);
				generator.Emit(OpCodes.Callvirt, temp.SetMethod);
			}

			generator.Emit(OpCodes.Ret);
			var clone = (Action<T, T>)dynamicMethod.CreateDelegate(typeof(Action<T, T>));
			CachedIl[typeof(T)] = clone;
			clone(source, destination);
		}

		// Function that clones an object into another object using IL (used to copy a base class into a derived class)
		public static T CloneFromWithIL<T>(this T destination, T source) {
			CloneObjectWithIL(source, destination);
			return destination;
		}
	}
}