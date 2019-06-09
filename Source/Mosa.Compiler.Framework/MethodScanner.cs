﻿// Copyright (c) MOSA Project. Licensed under the New BSD License.

using Mosa.Compiler.Common;
using Mosa.Compiler.Framework.Trace;
using Mosa.Compiler.MosaTypeSystem;
using System.Collections.Generic;
using System.Diagnostics;

namespace Mosa.Compiler.Framework
{
	public class MethodScanner
	{
		private const string UnitTestAttributeName = "Mosa.UnitTests.MosaUnitTestAttribute";
		private const int TraceLevel = 5;

		public bool IsEnabled { get; set; }

		private Compiler Compiler { get; }
		private MosaTypeLayout TypeLayout { get; }
		private TypeSystem TypeSystem { get; }

		private readonly HashSet<MosaType> allocatedTypes = new HashSet<MosaType>();
		private readonly HashSet<MosaMethod> invokedMethods = new HashSet<MosaMethod>();
		private readonly HashSet<MosaMethod> scheduledMethods = new HashSet<MosaMethod>();
		private readonly HashSet<MosaField> accessedFields = new HashSet<MosaField>();

		private readonly HashSet<MosaType> invokedInteraceTypes = new HashSet<MosaType>();
		private readonly KeyedList<MosaType, int> interfaceSlots = new KeyedList<MosaType, int>();

		private MosaMethod lastSource;

		private readonly TraceLog trace;

		private readonly object _lock = new object();

		public MethodScanner(Compiler compiler)
		{
			Compiler = compiler;
			TypeSystem = compiler.TypeSystem;
			TypeLayout = compiler.TypeLayout;
			IsEnabled = compiler.CompilerOptions.EnableMethodScanner;

			if (Compiler.CompilerTrace.IsTraceable(TraceLevel))
			{
				trace = new TraceLog(TraceType.GlobalDebug, null, null, "MethodScanner");
			}

			Initialize();
		}

		public void Complete()
		{
			if (!IsEnabled)
				return;

			if (!Compiler.CompilerOptions.EnableStatistics)
				return;

			MoreLogInfo();

			Debug.WriteLine(trace?.ToString()); // REMOVE

			int totalTypes = 0;
			int totalMethods = 0;

			foreach (var type in TypeSystem.AllTypes)
			{
				if (type.IsModule)
					continue;

				totalTypes++;

				foreach (var method in type.Methods)
				{
					if ((!(!method.HasImplementation && method.IsAbstract)) && !method.HasOpenGenericParams && !method.DeclaringType.HasOpenGenericParams)
					{
						totalMethods++;
					}
				}
			}

			Compiler.GlobalCounters.Update("MethodScanner.TotalTypes", totalTypes);
			Compiler.GlobalCounters.Update("MethodScanner.TotalMethods", totalMethods);

			Compiler.GlobalCounters.Update("MethodScanner.AllocatedTypes", allocatedTypes.Count);
			Compiler.GlobalCounters.Update("MethodScanner.InvokedMethods", invokedMethods.Count);
			Compiler.GlobalCounters.Update("MethodScanner.ScheduledMethods", scheduledMethods.Count);
			Compiler.GlobalCounters.Update("MethodScanner.AccessedFields", accessedFields.Count);
			Compiler.GlobalCounters.Update("MethodScanner.InvokedInterfaceType", invokedInteraceTypes.Count);

			Compiler.PostTrace(trace);
		}

		public void TypeAllocated(MosaType type, MosaMethod source)
		{
			if (!IsEnabled)
				return;

			lock (allocatedTypes)
			{
				if (allocatedTypes.Contains(type))
					return;

				allocatedTypes.Add(type);
			}

			if (trace != null)
			{
				lock (trace)
				{
					if ((lastSource == null && source != null) || (lastSource != source))
					{
						trace.Log($"> Method: {(source == null ? "[NULL]" : source.FullName)}");
						lastSource = source;
					}
					trace.Log($" >>> Allocated: {type.FullName}");
				}
			}

			lock (_lock)
			{
				Compiler.CompilerData.GetTypeData(type).IsTypeAllocated = true;

				// find all invoked methods for this type
				ScheduleMethods(type);
				ScheduleInterfaces(type);
			}
		}

		private void ScheduleInterfaces(MosaType type)
		{
			if (type.Interfaces.Count == 0)
				return;

			// find all interfaces methods for this type
			foreach (var itype in type.Interfaces)
			{
				if (!invokedInteraceTypes.Contains(itype))
					continue;

				var imethods = TypeLayout.GetInterfaceTable(type, itype);

				var list = interfaceSlots.Get(itype);

				foreach (var slot in list)
				{
					var imethod = imethods[slot];

					ScheduleMethod(imethod);
				}
			}
		}

		public void MethodInvoked(MosaMethod method, MosaMethod source)
		{
			if (!IsEnabled)
				return;

			MethodInvoked(method, source, false);
		}

		public void MethodDirectInvoked(MosaMethod method, MosaMethod source)
		{
			if (!IsEnabled)
				return;

			MethodInvoked(method, source, true);
			ScheduleMethod(method);
		}

		public void InterfaceMethodInvoked(MosaMethod method, MosaMethod source)
		{
			if (!IsEnabled)
				return;

			lock (invokedMethods)
			{
				if (invokedMethods.Contains(method))
					return;

				invokedMethods.Add(method);
			}

			if (trace != null)
			{
				lock (trace)
				{
					if ((lastSource == null && source != null) || (lastSource != source))
					{
						trace.Log($"> Method: {(source == null ? "[NONE]" : source.FullName)}");
						lastSource = source;
					}

					trace.Log($" >> Invoked: {method.FullName}{(method.IsStatic ? " [Static]" : " [Virtual]")}");
				}
			}

			lock (_lock)
			{
				invokedInteraceTypes.Add(method.DeclaringType);

				int slot = TypeLayout.GetMethodSlot(method);
				var interfaceType = method.DeclaringType;

				interfaceSlots.AddIfNew(interfaceType, slot);

				// For every allocated type that implements this interface method, schedule the type's interface method
				foreach (var type in TypeSystem.AllTypes)
				{
					if (type.IsInterface)
						continue;

					if (!type.Interfaces.Contains(interfaceType))
						continue;

					lock (allocatedTypes)
					{
						if (!allocatedTypes.Contains(type))
							continue;
					}

					var imethods = TypeLayout.GetInterfaceTable(type, interfaceType); // this can be slow

					var imethod = imethods[slot];

					// schedule this type's interface method implementation
					ScheduleMethod(imethod);
				}
			}
		}

		private void MethodInvoked(MosaMethod method, MosaMethod source, bool direct)
		{
			if (!IsEnabled)
				return;

			lock (invokedMethods)
			{
				if (invokedMethods.Contains(method))
					return;

				invokedMethods.Add(method);
			}

			if (trace != null)
			{
				lock (trace)
				{
					if ((lastSource == null && source != null) || (lastSource != source))
					{
						trace.Log($"> Method: {(source == null ? "[NONE]" : source.FullName)}");
						lastSource = source;
					}

					trace.Log($" >> Invoked: {method.FullName}{(method.IsStatic ? " [Static]" : " [Virtual]")}");
				}
			}

			lock (_lock)
			{
				if (method.IsStatic || method.IsConstructor || method.DeclaringType.IsValueType || direct)
				{
					ScheduleMethod(method);
				}
				else
				{
					bool contains;

					lock (allocatedTypes)
					{
						contains = allocatedTypes.Contains(method.DeclaringType);
					}

					if (contains)
					{
						ScheduleMethod(method);
					}

					var slot = TypeLayout.GetMethodSlot(method);

					ScheduleDerivedMethods(method.DeclaringType, slot);
				}
			}
		}

		private void ScheduleDerivedMethods(MosaType type, int slot)
		{
			var children = TypeLayout.GetDerivedTypes(type);

			if (children == null)
				return;

			foreach (var derived in children)
			{
				bool contains;

				lock (allocatedTypes)
				{
					contains = allocatedTypes.Contains(derived);
				}

				if (contains)
				{
					var derivedMethod = TypeLayout.GetMethodBySlot(derived, slot);

					lock (invokedMethods)
					{
						if (!invokedMethods.Contains(derivedMethod))
						{
							invokedMethods.Add(derivedMethod);
						}
					}

					ScheduleMethod(derivedMethod);
				}

				ScheduleDerivedMethods(derived, slot);
			}
		}

		private void ScheduleMethods(MosaType type)
		{
			var currentType = type;

			var slots = new bool[TypeLayout.GetMethodTable(type).Count];

			while (currentType != null)
			{
				foreach (var method in currentType.Methods)
				{
					bool contains;

					lock (invokedMethods)
					{
						contains = invokedMethods.Contains(method);
					}

					if (contains)
					{
						int slot = TypeLayout.GetMethodSlot(method);

						if (slots[slot])
							continue;

						slots[slot] = true;

						ScheduleMethod(method);
					}
				}

				currentType = currentType.BaseType; // EXPLORE: base types may not need to be considered
			}
		}

		private void ScheduleMethod(MosaMethod method)
		{
			lock (scheduledMethods)
			{
				if (scheduledMethods.Contains(method))
					return;

				if (method.FullName.Contains("Mosa.UnitTests.GenericInterfaceTestClass`1<System.Int32>::Mosa.UnitTests.IInterfaceBB<T>.Get"))
				{
					trace?.Log("TEST");
				}

				scheduledMethods.Add(method);

				trace?.Log($" ==> Scheduling: {method}{(method.IsStatic ? " [Static]" : " [Virtual]")}");

				Compiler.MethodScheduler.Schedule(method);
			}
		}

		public void Initialize()
		{
			if (!IsEnabled)
				return;

			var entryPoint = TypeSystem.EntryPoint;

			if (entryPoint != null)
			{
				invokedMethods.Add(entryPoint);
				ScheduleMethod(entryPoint);
			}

			var objectType = TypeSystem.GetTypeByName("System", "Object");
			allocatedTypes.Add(objectType);

			var stringType = TypeSystem.GetTypeByName("System", "String");
			allocatedTypes.Add(stringType);

			var typeType = TypeSystem.GetTypeByName("System", "Type");
			allocatedTypes.Add(typeType);

			var exceptionType = TypeSystem.GetTypeByName("System", "Exception");
			allocatedTypes.Add(exceptionType);

			var delegateType = TypeSystem.GetTypeByName("System", "Delegate");
			allocatedTypes.Add(delegateType);

			//var arrayType = TypeSystem.GetTypeByName("System", "Array");
			//allocatedTypes.Add(arrayType);

			// Collect all unit tests methods
			foreach (var type in TypeSystem.AllTypes)
			{
				bool allocateType = false;

				foreach (var method in type.Methods)
				{
					if (!method.IsStatic)
						continue;

					var methodAttribute = method.FindCustomAttribute(UnitTestAttributeName);

					if (methodAttribute != null)
					{
						invokedMethods.Add(method);
						ScheduleMethod(method);
						allocateType = true;
					}
				}

				if (allocateType)
				{
					TypeAllocated(type, null);
				}
			}
		}

		public void AccessedField(MosaField field)
		{
			if (!IsEnabled)
				return;

			if (!field.IsStatic)
				return;

			lock (accessedFields)
			{
				accessedFields.AddIfNew(field);
			}
		}

		public bool IsFieldAccessed(MosaField field)
		{
			if (!IsEnabled)
				return true; // always

			return accessedFields.Contains(field);
		}

		public bool IsTypeAllocated(MosaType type)
		{
			if (!IsEnabled)
				return true; // always

			return allocatedTypes.Contains(type);
		}

		public bool IsMethodInvoked(MosaMethod method)
		{
			if (!IsEnabled)
				return true; // always

			return invokedMethods.Contains(method);
		}

		public void MoreLogInfo()
		{
			foreach (var type in TypeSystem.AllTypes)
			{
				if (type.IsModule)
					continue;

				if (!IsTypeAllocated(type))
				{
					trace?.Log($"Type Excluded: {type.FullName}");
				}

				foreach (var method in type.Methods)
				{
					if (!IsMethodInvoked(method))
					{
						trace?.Log($"Method Excluded: {method.FullName}");
					}
				}
			}
		}
	}
}
