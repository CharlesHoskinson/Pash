﻿// Copyright (C) Pash Contributors. License: GPL/BSD. See https://github.com/Pash-Project/Pash/
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Management.Automation.Runspaces;
using System.Reflection;
using System.Text;
using Pash.Implementation;
using System.Management.Automation;

namespace System.Management.Automation
{
    internal class CommandProcessor : CommandProcessorBase
    {
        internal Cmdlet Command { get; set; }
        readonly CmdletInfo _cmdletInfo;
        bool _beganProcessing;

        public CommandProcessor(CmdletInfo cmdletInfo)
            : base(cmdletInfo)
        {
            _cmdletInfo = cmdletInfo;
            _beganProcessing = false;
        }

        internal override ICommandRuntime CommandRuntime
        {
            get
            {
                return Command.CommandRuntime;
            }
            set
            {
                Command.CommandRuntime = value;
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="obj"></param>
        /// <remarks>
        /// All abount Cmdlet parameters: http://msdn2.microsoft.com/en-us/library/ms714433(VS.85).aspx
        /// </remarks>
        internal override void BindArguments(PSObject obj)
        {
            // TODO: Bind obj properties to ValueFromPipelinebyPropertyName parameters
            // TODO: If parameter has ValueFromRemainingArguments any unmatched arguments should be bound to this parameter as an array
            // TODO: If no parameter has ValueFromRemainingArguments any supplied parameters are unmatched then fail with exception

            if ((obj == null) && (Parameters.Count == 0))
                return;

            IEnumerable<string> namedParameters = Parameters.Select (x => x.Name).Where (x => !string.IsNullOrEmpty (x));
            CommandParameterSetInfo paramSetInfo = _cmdletInfo.GetDefaultParameterSet();
            IDictionary<string, CommandParameterInfo> namedParametersLookup = paramSetInfo.LookupAllParameters(namedParameters);
            IEnumerable<KeyValuePair<string, CommandParameterInfo>> unknownParameters = namedParametersLookup.ToList ().Where(x => x.Value == null);
            if (unknownParameters.Any ())
            {
                // Cannot find all named parameters in default parameter set.
                // Lets try other parameter sets.
                bool found = false;
                foreach (CommandParameterSetInfo nonDefaultParamSetInfo in _cmdletInfo.GetNonDefaultParameterSets())
                {
                    namedParametersLookup = nonDefaultParamSetInfo.LookupAllParameters(namedParameters);
                    if (!namedParametersLookup.Values.Where (x => x == null).Any ())
                    {
                        paramSetInfo = nonDefaultParamSetInfo;
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    throw new ParameterBindingException("No parameter found matching '" + unknownParameters.First().Key + "'");
                }
            }

            if (obj != null)
            {
                var valueFromPipelineParameter = paramSetInfo.Parameters.Where(paramInfo => paramInfo.ValueFromPipeline).SingleOrDefault();

                if (valueFromPipelineParameter != null)
                    BindArgument(valueFromPipelineParameter.Name, obj, valueFromPipelineParameter.ParameterType);
            }

            if (Parameters.Count > 0)
            {
                // bind by position location
                for (int i = 0; i < Parameters.Count; i++)
                {
                    CommandParameterInfo paramInfo = null;

                    CommandParameter parameter = Parameters[i];

                    if (string.IsNullOrEmpty(parameter.Name))
                    {
                        paramInfo = paramSetInfo.GetParameterByPosition(i);

                        if (paramInfo != null)
                        {
                            BindArgument(paramInfo.Name, parameter.Value, paramInfo.ParameterType);
                        }
                    }
                    else
                    {
                        namedParametersLookup.TryGetValue(parameter.Name, out paramInfo);
                        if (parameter.Value == null && paramInfo.ParameterType != typeof(SwitchParameter)
                            && i < Parameters.Count - 1 && Parameters[i + 1].Name == null)
                        {
                            BindArgument(paramInfo.Name, Parameters[i + 1].Value, paramInfo.ParameterType);
                            i++;
                        }
                        else
                            BindArgument(paramInfo.Name, parameter.Value, paramInfo.ParameterType);
                    }
                }
            }
        }

        private void BindArgument(string name, object value, Type type)
        {
            Type memberType = null;

            // Look for Property to bind to
            MemberInfo memberInfo = Command.GetType().GetProperty(name, type);

            if (memberInfo != null)
            {
                memberType = ((PropertyInfo)memberInfo).PropertyType;
            }
            else  // No property found try bind to field instead
            {
                memberInfo = Command.GetType().GetField(name);

                if (memberInfo != null)
                {
                    memberType = ((FieldInfo)memberInfo).FieldType;
                }
                else
                {
                    throw new Exception("Unable to get field or property named: " + name);
                }
            }

            // TODO: make this generic
            if (memberType == typeof(PSObject[]))
            {
                SetValue(memberInfo, Command, new[] { PSObject.AsPSObject(value) });
            }

            else if (memberType == typeof(String[]))
            {
                SetValue(memberInfo, Command, ConvertToStringArray(value));
            }

            else if (memberType == typeof(String))
            {
                SetValue(memberInfo, Command, value.ToString());
            }

            else if (memberType.IsEnum) {
                  SetValue (memberInfo, Command, Enum.Parse (type, value.ToString(), true));
            }

            else if (memberType == typeof(PSObject))
            {
                SetValue(memberInfo, Command, PSObject.AsPSObject(value));
            }

            else if (memberType == typeof(SwitchParameter))
            {
                SetValue(memberInfo, Command, new SwitchParameter(true));
            }

            else if (memberType == typeof(Object[]))
            {
                SetValue(memberInfo, Command, new[] { value });
            }

            else
            {
                SetValue(memberInfo, Command, value is PSObject ? ((PSObject)value).BaseObject : value);
            }
        }

        private object ConvertToStringArray(object value)
        {
            if ((value is IEnumerable) && !(value is string))
            {
                return (from object item in (IEnumerable)value
                        select item.ToString()).ToArray();
            }
            else
            {
                return new[] { value.ToString() };
            }
        }

        public static void SetValue(MemberInfo info, object targetObject, object value)
        {
            if (info.MemberType == MemberTypes.Field)
            {
                FieldInfo fieldInfo = info as FieldInfo;
                fieldInfo.SetValue(targetObject, value);
            }

            else if (info.MemberType == MemberTypes.Property)
            {
                PropertyInfo propertyInfo = info as PropertyInfo;
                propertyInfo.SetValue(targetObject, value, null);
            }

            else throw new Exception("SetValue only implemented for fields and properties");
        }

        internal override void Initialize()
        {
            try
            {
                Cmdlet cmdlet = (Cmdlet)Activator.CreateInstance(_cmdletInfo.ImplementingType);

                cmdlet.CommandInfo = _cmdletInfo;
                cmdlet.ExecutionContext = base.ExecutionContext;
                Command = cmdlet;
            }
            catch (Exception e)
            {
                // TODO: work out the failure
                System.Console.WriteLine(e);
            }
        }

        internal override void ProcessRecord()
        {
            // TODO: initialize Cmdlet parameters
            if (!_beganProcessing)
            {
                Command.DoBeginProcessing();
                _beganProcessing = true;
            }

            Command.DoProcessRecord();
        }

        internal void ProcessObject(object inObject)
        {
            /*
            PSObject inPsObject = null;
            if (inputObject != null)
            {
                inPsObject = PSObject.AsPSObject(inObject);
            }
            */
        }

        internal override void Complete()
        {
            Command.DoEndProcessing();
        }
    }
}
