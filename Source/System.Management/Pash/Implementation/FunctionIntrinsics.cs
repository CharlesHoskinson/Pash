using System;
using System.Management.Automation;
using System.Collections.ObjectModel;
using System.Collections.Generic;

namespace Pash.Implementation
{
    internal sealed class FunctionIntrinsics
    {
        private SessionState _sessionState;
        private SessionStateScope<FunctionInfo> _scope;

        internal FunctionIntrinsics(SessionState sessionState, SessionStateScope<FunctionInfo> functionScope)
        {
            _sessionState = sessionState;
            _scope = functionScope;
        }

        public FunctionInfo Get(string functionName)
        {
            return _scope.Get(functionName, true);
        }

        internal Dictionary<string, FunctionInfo> GetAll()
        {
            /*
             * "Fun" fact: The GetAll() function of all *Intrinsics classes is perfect to be used
             * by the provider when "Get-ChildItem" is called for example. With PS2.0, alias and variable
             * providers return all elements that are useable and visible in the current scope.
             * But not the FunctionProvider. It returns even inaccessible functions, e.g. those that are private
             * in a parent scope. They cannot be called, but are returned. This behavior is just bad, therefore
             * I won't implement it. I will implement this the same way as for aliases and variables.
             */
            return _scope.GetAll();
        }

        public void Set(string name, ScriptBlock function, string description = "")
        {
            var qualName = new SessionStateScope<FunctionInfo>.QualifiedName(name);
            var info = new FunctionInfo(qualName.UnqualifiedName, function);
            info.Description = description;
            _scope.Set(name, info, true, true);
        }

        public void Set(FunctionInfo info)
        {
            _scope.SetAtScope(info, "local", true);
        }

        public void Remove(string name)
        {
            _scope.Remove(name, true);
        }

    }
}

