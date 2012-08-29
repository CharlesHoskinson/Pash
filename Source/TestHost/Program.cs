﻿using System;
using System.Linq;
using System.Management.Automation.Runspaces;
using Pash.Implementation;
using System.Management.Automation.Host;
using System.Diagnostics;
using System.Collections.ObjectModel;
using System.Management.Automation;

namespace TestHost
{
    class Program
    {
        static void Main(string[] args)
        {
            VerifyExpression("xxx", "'xxx'");
            VerifyExpression("xxx1", "'xxx' + 1");
            VerifyExpression(3, "1 + 2");
        }

        private static void VerifyExpression<T>(T Expected, string Expression)
        {
            Debug.Assert(object.Equals((T)Execute(Expression).Single().ImmediateBaseObject, Expected));
        }

        private static Collection<PSObject> Execute(string statement)
        {
            var myHost = new TestHost();
            var myRunSpace = RunspaceFactory.CreateRunspace(myHost);
            myRunSpace.Open();

            using (var currentPipeline = myRunSpace.CreatePipeline())
            {
                currentPipeline.Commands.Add(statement);
                return currentPipeline.Invoke();
            }
        }
    }

    class TestHost : PSHost
    {
        public override System.Globalization.CultureInfo CurrentCulture
        {
            get { throw new NotImplementedException(); }
        }

        public override System.Globalization.CultureInfo CurrentUICulture
        {
            get { throw new NotImplementedException(); }
        }

        public override Guid InstanceId
        {
            get { throw new NotImplementedException(); }
        }

        public override string Name
        {
            get { throw new NotImplementedException(); }
        }

        public override PSHostUserInterface UI
        {
            get { throw new NotImplementedException(); }
        }

        public override Version Version
        {
            get { throw new NotImplementedException(); }
        }

        public override void EnterNestedPrompt()
        {
            throw new NotImplementedException();
        }

        public override void ExitNestedPrompt()
        {
            throw new NotImplementedException();
        }

        public override void NotifyBeginApplication()
        {
            throw new NotImplementedException();
        }

        public override void NotifyEndApplication()
        {
            throw new NotImplementedException();
        }

        public override void SetShouldExit(int exitCode)
        {
            throw new NotImplementedException();
        }
    }

}