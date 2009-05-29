//Copyright (c) 2003, Rich Hickey
//licensed under the BSD license - see license.txt
using System;
using System.Reflection;

namespace DotLisp
{


    internal class CLSMethod : CLSMember
    {
        private Type type;
        private MemberInfo[] methods;
        private Boolean isStatic;

        internal CLSMethod(String name, Type type, MemberInfo[] methods, Boolean isStatic)
        {
            this.isStatic = isStatic;
            this.type = type;
            this.methods = methods;
            this.name = name;
        }

        public override Object Invoke(params Object[] args)
        {
            try
            {
                return Invoke0(args);
            }
            catch (Exception e)
            {
                Console.WriteLine("" + e);
                return Invoke0(args);
            }

        }

        static Binder methodBinder = System.Type.DefaultBinder;

        //
        // Summary:
        //     Invokes the specified member, using the specified binding constraints and
        //     matching the specified argument list.
        //
        // Parameters:
        //   name:
        //     The System.String containing the name of the constructor, method, property,
        //     or field member to invoke.-or- An empty string ("") to invoke the default
        //     member. -or-For IDispatch members, a string representing the DispID, for
        //     example "[DispID=3]".
        //
        //   invokeAttr:
        //     A bitmask comprised of one or more System.Reflection.BindingFlags that specify
        //     how the search is conducted. The access can be one of the BindingFlags such
        //     as Public, NonPublic, Private, InvokeMethod, GetField, and so on. The type
        //     of lookup need not be specified. If the type of lookup is omitted, BindingFlags.Public
        //     | BindingFlags.Instance | BindingFlags.Static are used.
        //
        //   binder:
        //     A System.Reflection.Binder object that defines a set of properties and enables
        //     binding, which can involve selection of an overloaded method, coercion of
        //     argument types, and invocation of a member through reflection.-or- null,
        //     to use the System.Type.DefaultBinder. Note that explicitly defining a System.Reflection.Binder
        //     object may be requird for successfully invoking method overloads with variable
        //     arguments.
        //
        //   target:
        //     The System.Object on which to invoke the specified member.
        //
        //   args:
        //     An array containing the arguments to pass to the member to invoke.
        //
        // Returns:
        //     An System.Object representing the return value of the invoked member.
        //
        // Exceptions:
        //   System.ArgumentNullException:
        //     invokeAttr contains CreateInstance and typeName is null.
        //
        //   System.ArgumentException:
        //     args is multidimensional.-or- invokeAttr is not a valid System.Reflection.BindingFlags
        //     attribute.-or- invokeAttr contains CreateInstance combined with InvokeMethod,
        //     GetField, SetField, GetProperty, or SetProperty.-or- invokeAttr contains
        //     both GetField and SetField.-or- invokeAttr contains both GetProperty and
        //     SetProperty.-or- invokeAttr contains InvokeMethod combined with SetField
        //     or SetProperty.-or- invokeAttr contains SetField and args has more than one
        //     element.-or- This method is called on a COM object and one of the following
        //     binding flags was not passed in: BindingFlags.InvokeMethod, BindingFlags.GetProperty,
        //     BindingFlags.SetProperty, BindingFlags.PutDispProperty, or BindingFlags.PutRefDispProperty.-or-
        //     One of the named parameter arrays contains a string that is null.
        //
        //   System.MethodAccessException:
        //     The specified member is a class initializer.
        //
        //   System.MissingFieldException:
        //     The field or property cannot be found.
        //
        //   System.MissingMethodException:
        //     The method cannot be found.-or- The current System.Type object represents
        //     a type that contains open type parameters, that is, System.Type.ContainsGenericParameters
        //     returns true.
        //
        //   System.Reflection.TargetException:
        //     The specified member cannot be invoked on target.
        //
        //   System.Reflection.AmbiguousMatchException:
        //     More than one method matches the binding criteria.
        //
        //   System.NotSupportedException:
        //     The .NET Compact Framework does not currently support this method.
        //
        //   System.InvalidOperationException:
        //     The method represented by name has one or more unspecified generic type parameters.
        //     That is, the method's System.Reflection.MethodInfo.ContainsGenericParameters
        //     property returns true.
        public Object Invoke0(params Object[] args)
        {
            Object target = null;
            Object[] argarray = args;
            if (!isStatic)
            {				 // instance field gets target from first arg
                // MEH: More informative exception.
                if (args.Length == 0)
                    throw new Exception(".Type:Method requires a target.");

                target = args[0];
                argarray = Util.vector_rest(args);
            }
            MethodInfo mi = null;
            Exception lastException = null;
            if (methods.Length == 1)	//it's not overloaded
            {
                mi = (MethodInfo)methods[0];
                try
                {
                    ParameterInfo[] paramInfos = mi.GetParameters();
                    if (argarray.Length == paramInfos.Length)
                    {
                        return mi.Invoke(target, argarray);
                    }
                }
                catch (Exception e)
                {
                    lastException = e;
                }
            }

            //this should always work, but seems to have problems, i.e. String.Concat
            if (Util.containsNull(argarray))
            {
                try
                {
                    return type.InvokeMember(this.name,
                        /*BindingFlags.Public |*/ BindingFlags.InvokeMethod//|BindingFlags.FlattenHierarchy
                                                     | (isStatic ? BindingFlags.Static : BindingFlags.Instance)
                                                     , null, target, argarray);
                }
                catch (Exception e)
                {
                    Console.WriteLine("ignoring " + e.ToString());
                }
            }
            ///*
            Type[] argtypes = Type.GetTypeArray(argarray);
            //todo cache result?
            //n.b. we are not specifying static/instance here - hmmm...
            mi = type.GetMethod(name, argtypes);
            // found method
            try
            {
                if (mi != null) return mi.Invoke(target, argarray);
            }
            catch (SystemException e)
            {
                lastException = e;
                Console.WriteLine("ignoring " + e.ToString());
            }
            try
            {
                return type.InvokeMember(this.name,
                    /*BindingFlags.Public |*/ BindingFlags.InvokeMethod//|BindingFlags.FlattenHierarchy
                                                 | (isStatic ? BindingFlags.Static : BindingFlags.Instance)
                                                 , methodBinder, target, argarray);
            }
            // Exceptions:
            catch (System.ArgumentNullException e) { lastException = e; }
            //   System.ArgumentNullException:
            //     invokeAttr contains CreateInstance and typeName is null.
            //
            catch (System.ArgumentException e) { lastException = e; }

            //   System.ArgumentException:
            //     args is multidimensional.-or- invokeAttr is not a valid System.Reflection.BindingFlags
            //     attribute.-or- invokeAttr contains CreateInstance combined with InvokeMethod,
            //     GetField, SetField, GetProperty, or SetProperty.-or- invokeAttr contains
            //     both GetField and SetField.-or- invokeAttr contains both GetProperty and
            //     SetProperty.-or- invokeAttr contains InvokeMethod combined with SetField
            //     or SetProperty.-or- invokeAttr contains SetField and args has more than one
            //     element.-or- This method is called on a COM object and one of the following
            //     binding flags was not passed in: BindingFlags.InvokeMethod, BindingFlags.GetProperty,
            //     BindingFlags.SetProperty, BindingFlags.PutDispProperty, or BindingFlags.PutRefDispProperty.-or-
            //     One of the named parameter arrays contains a string that is null.
            //
            catch (System.MethodAccessException e) { lastException = e; }
            //     The specified member is a class initializer.
            //
            catch (System.MissingFieldException e) { lastException = e; }
            //     The field or property cannot be found.
            //
            catch (System.MissingMethodException) { }
            //     The method cannot be found.-or- The current System.Type object represents
            //     a type that contains open type parameters, that is, System.Type.ContainsGenericParameters
            //     returns true.
            //
            catch (System.Reflection.TargetException e) { lastException = e; }
            //     The specified member cannot be invoked on target.
            //
            catch (System.Reflection.AmbiguousMatchException e) { lastException = e; }
            //     More than one method matches the binding criteria.
            //
            catch (System.NotSupportedException e) { lastException = e; }
            //     The .NET Compact Framework does not currently support this method.
            //
            catch (System.InvalidOperationException e) { lastException = e; }
            //     The method represented by name has one or more unspecified generic type parameters.
            //     That is, the method's System.Reflection.MethodInfo.ContainsGenericParameters
            //     property returns true.

            object[] parameters;
            foreach (MethodInfo m in methods)
            {
                try
                {
                    ParameterInfo[] paramInfos = m.GetParameters();
                    if (Coerce(argarray, argtypes, paramInfos, out parameters))
                        return m.Invoke(target, parameters);
                }
                catch (Exception e)
                {
                    lastException = e;
                }
            }
            if (lastException != null)
            {
                Console.WriteLine("rethrowing " + lastException.ToString());
                throw lastException;
            }

            throw new Exception("Can't find matching method: " + name + " for: " + type.Name +
                                      " taking those arguments");
            //*/


        }

        private bool Coerce(object[] argarray, Type[] argtypes, ParameterInfo[] paramInfos, out object[] parameters)
        {
            int paramInfosLength = paramInfos.Length;
            int argarrayLength = argarray.Length;
            if (paramInfosLength == argarrayLength)
            {
                parameters = argarray;
                return true;
            }
            if (paramInfosLength > argarrayLength)
            {
                parameters = null;
                return false;
            }
            parameters = new object[paramInfosLength];
            int currentParam = 0;
            for (int i = 0; i < paramInfosLength; i++)
            {
                ParameterInfo p = paramInfos[i];
                if (currentParam > argarrayLength)
                {
                    parameters[i] = p.DefaultValue;
                }
                else if (p.ParameterType.IsArray)
                {
                    if (!argtypes[currentParam].IsArray)
                    {
                        // the last arg is an array fill it with the rest and return
                        if (i + 1 == paramInfosLength)
                        {
                            object[] pas = new object[argarrayLength - currentParam];
                            parameters[i] = pas;
                            i = 0;
                            while (currentParam < argarrayLength)
                            {
                                pas[i++] = argarray[currentParam++];
                            }
                            return true;
                        }                        
                    }
                   
                }
                else
                {
                    parameters[i] = argarray[currentParam];
                }
                currentParam++;
            }
            return true;
        }

    }
}
