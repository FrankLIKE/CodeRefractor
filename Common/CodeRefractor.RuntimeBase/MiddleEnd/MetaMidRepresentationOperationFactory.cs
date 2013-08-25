using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using CodeRefractor.RuntimeBase.Analyze;
using CodeRefractor.RuntimeBase.MiddleEnd.Methods;
using CodeRefractor.RuntimeBase.MiddleEnd.SimpleOperations;
using CodeRefractor.RuntimeBase.MiddleEnd.SimpleOperations.ConstTable;
using CodeRefractor.RuntimeBase.MiddleEnd.SimpleOperations.Identifiers;
using CodeRefractor.RuntimeBase.MiddleEnd.SimpleOperations.Operators;
using CodeRefractor.RuntimeBase.Shared;
using Mono.Reflection;

namespace CodeRefractor.RuntimeBase.MiddleEnd
{
    public class MetaMidRepresentationOperationFactory
    {
        private readonly MetaMidRepresentation _representation;
        private readonly EvaluatorStack _evaluator;

        public MetaMidRepresentationOperationFactory(MetaMidRepresentation representation, EvaluatorStack evaluator)
        {
            _representation = representation;
            _evaluator = evaluator;
        }


        private void AddOperation(LocalOperation.Kinds kind, object value = null)
        {
            var result = new LocalOperation
            {
                Kind = kind,
                Value = value
            };

            _representation.LocalOperations.Add(result);
            var assignment = result.Value as Assignment;
            if (assignment != null)
            {
                if (assignment.AssignedTo.FixedType == null)
                    throw new InvalidOperationException(
                        String.Format("The data introduced in the IR should be well typed. " +
                                      Environment.NewLine + "Operation: {0}", result));
            }
        }
        public LocalVariable SetNewVReg()
        {
            var newLocal = _evaluator.SetNewVReg();
            _representation.Vars.VirtRegs.Add(newLocal);
            return newLocal;
        }

        private void AssignValueToStack(object value)
        {
            var result = SetNewVReg();
            var assign = new Assignment
            {
                AssignedTo = result,
                Right = new ConstValue(value)
            };
            result.FixedType = value.GetType();
            AddOperation(LocalOperation.Kinds.Assignment, assign);
        }

        public void CopyLocalVariableIntoStack(int value)
        {
            var locVar = _representation.Vars.LocalVariables[value];

            var vreg = SetNewVReg();

            var assingment = new Assignment
                                 {
                                     AssignedTo = vreg,
                                     Right = locVar
                                 };

            vreg.FixedType = locVar.ComputedType();
            AddOperation(LocalOperation.Kinds.Assignment, assingment);
        }


        public void PushInt4(int value)
        {
            AssignValueToStack(value);
        }

        public void PushString(string value)
        {
            AssignValueToStack(value);
        }

        public void PushDouble(double value)
        {
            AssignValueToStack(value);
        }

        public void LoadArgument(int pushedIntValue)
        {
            var argument = _representation.Vars.Arguments[pushedIntValue];
            var vreg = SetNewVReg();
            vreg.FixedType = argument.ComputedType();
            var assignment = new Assignment
            {
                AssignedTo = vreg,
                Right = argument
            };
            AddOperation(LocalOperation.Kinds.Assignment, assignment);
        }

        public void CopyStackIntoArgument(int value)
        {
            var topVariable = _evaluator.Stack.Peek();
            var newLocal = new LocalVariable
            {
                Kind = VariableKind.Argument,
                Id = value
            };

            _evaluator.Stack.Pop();
            _representation.Vars.LocalVariables[value] = newLocal;
            var assingment = new Assignment
            {
                AssignedTo = newLocal,
                Right = topVariable
            };

            newLocal.FixedType = topVariable.ComputedType();
            AddOperation(LocalOperation.Kinds.Assignment, assingment);
        }
        public void CopyStackIntoLocalVariable(int value)
        {
            var topVariable = _evaluator.Stack.Peek();
            var newLocal = new LocalVariable
            {
                Kind = VariableKind.Local,
                Id = value
            };

            _evaluator.Stack.Pop();
            _representation.Vars.LocalVariables[value] = newLocal;
            var assingment = new Assignment
            {
                AssignedTo = newLocal,
                Right = topVariable
            };

            newLocal.FixedType = topVariable.ComputedType();
            AddOperation(LocalOperation.Kinds.Assignment, assingment);
        }

        public void StoreField(FieldInfo fieldInfo)
        {
            var secondVar = _evaluator.Stack.Pop();
            var firstVar = _evaluator.Stack.Pop();
            var fieldName = fieldInfo.Name;
            var assignment = new Assignment
            {
                AssignedTo = new FieldSetter
                {
                    Instance = firstVar,
                    FieldName = fieldName
                },
                Right = secondVar
            };
            assignment.AssignedTo.FixedType = secondVar.ComputedType();
            AddOperation(LocalOperation.Kinds.SetField, assignment);
        }

        public void LoadReferenceInArray()
        {
            var secondVar = _evaluator.Stack.Pop();
            var firstVar = _evaluator.Stack.Pop();

            var result = SetNewVReg();
            result.FixedType = firstVar.FixedType.GetElementType();
            var arrayVariable = new ArrayVariable(firstVar, secondVar);
            var assignment = new Assignment
            {
                AssignedTo = result,
                Right = arrayVariable
            };
            result.FixedType = arrayVariable.GetElementType();
            AddOperation(LocalOperation.Kinds.GetArrayItem, assignment);
        }

        public void Return(bool isVoid)
        {
            var returnValue = isVoid ? null : _evaluator.Stack.Pop();
            AddOperation(LocalOperation.Kinds.Return, returnValue);
        }


        private void SetUnaryOperator(string operatorName)
        {
            var firstVar = _evaluator.Stack.Pop();
            var result = SetNewVReg();


            var assign = new UnaryOperator(operatorName)
            {
                AssignedTo = result,
                Left = firstVar,
            };
            assign.AssignedTo.FixedType = firstVar.ComputedType();
            AddOperation(LocalOperation.Kinds.UnaryOperator, assign);
        }

        public void ConvI4()
        {
            SetUnaryOperator(OpcodeOperatorNames.ConvI4);
            _evaluator.Top.FixedType = typeof (int);
        }

        public void Not()
        {
            SetUnaryOperator(OpcodeOperatorNames.Not);
        }

        public void Neg()
        {
            SetUnaryOperator(OpcodeOperatorNames.Neg);
        }


        public void LoadLength()
        {
            SetUnaryOperator(OpcodeOperatorNames.LoadLen);

            _evaluator.Top.FixedType = typeof(int);
        }

        public void ConvI8()
        {
            SetUnaryOperator(OpcodeOperatorNames.ConvI8);
            _evaluator.Top.FixedType = typeof(Int64);
        }

        public void ConvR4()
        {
            SetUnaryOperator(OpcodeOperatorNames.ConvR4);
            _evaluator.Top.FixedType = typeof(float);
        }

        public void ConvR8()
        {
            SetUnaryOperator(OpcodeOperatorNames.ConvR8);
            _evaluator.Top.FixedType = typeof(double);
        }


        public void Dup()
        {
            var variable = _evaluator.Stack.Peek();
            var vreg = SetNewVReg();
            var assign = new Assignment
            {
                AssignedTo = vreg,
                Right = variable
            };
            vreg.FixedType = variable.ComputedType();
            AddOperation(LocalOperation.Kinds.Assignment, assign);
        }
        public void Pop()
        {
            _evaluator.Stack.Pop();
        }



        private void SetBinaryOperator(string operatorName)
        {
            var secondVar = _evaluator.Stack.Pop();
            var firstVar = _evaluator.Stack.Pop();

            var result = SetNewVReg();
            var assign = new BinaryOperator(operatorName)
            {
                AssignedTo = result,
                Left = firstVar,
                Right = secondVar
            };
            result.FixedType = assign.ComputedType();
            AddOperation(LocalOperation.Kinds.BinaryOperator, assign);
        }

        public void SetLabel(int offset)
        {
            AddOperation(LocalOperation.Kinds.Label, offset);
        }
        public void Cgt()
        {
            SetBinaryOperator("cgt");
        }

        public void Ceq()
        {
            SetBinaryOperator("ceq");
        }

        public void Clt()
        {
            SetBinaryOperator("clt");
        }

        #region Operators

        public void Add()
        {
            SetBinaryOperator(OpcodeOperatorNames.Add);
        }

        public void Sub()
        {
            SetBinaryOperator(OpcodeOperatorNames.Sub);
        }

        public void Div()
        {
            SetBinaryOperator(OpcodeOperatorNames.Div);
        }

        public void Rem()
        {
            SetBinaryOperator(OpcodeOperatorNames.Rem);
        }

        public void Mul()
        {
            SetBinaryOperator(OpcodeOperatorNames.Mul);
        }

        public void And()
        {
            SetBinaryOperator(OpcodeOperatorNames.And);
        }

        public void Or()
        {
            SetBinaryOperator(OpcodeOperatorNames.Or);
        }

        public void Xor()
        {
            SetBinaryOperator(OpcodeOperatorNames.Xor);
        }

        #endregion

        public static bool HandleRuntimeHelpersMethod(MethodBase method)
        {
            var declType = method.DeclaringType;
            return declType == typeof(RuntimeHelpers);
        }


        public void Call(object operand)
        {
            var methodInfo = (MethodBase)operand;
            var methodData = new MethodData(methodInfo);


            if (HandleRuntimeHelpersMethod(_representation.Method))
            {
                methodData.ExtractNeededValuesFromStack(_evaluator);
                AddOperation(LocalOperation.Kinds.CallRuntime, methodData);
                return;
            }
            methodData.ExtractNeededValuesFromStack(_evaluator);
            if (!methodData.IsVoid)
            {
                var vreg = SetNewVReg();
                vreg.FixedType = methodInfo.GetReturnType();
                methodData.Result = vreg;
            }
            methodData.FixedType = methodInfo.GetReturnType();
            AddOperation(LocalOperation.Kinds.Call, methodData);
        }


        public void StoreStaticField(FieldInfo fieldInfo)
        {
            var firstVar = _evaluator.Stack.Pop();
            var fieldName = fieldInfo.Name;
            var assignment = new Assignment
            {
                AssignedTo = new StaticFieldSetter
                {
                    DeclaringType = fieldInfo.DeclaringType,
                    FieldName = fieldName
                },
                Right = firstVar
            };
            assignment.AssignedTo.FixedType = firstVar.ComputedType();
            AddOperation(LocalOperation.Kinds.SetStaticField, assignment);
        }


        public void StoresValueFromAddress(LocalVariableInfo index)
        {
            var varAddress = _evaluator.Stack.Pop();
            var varValue = _evaluator.Stack.Pop();

            var assignment = new DerefAssignment
            {
                Left = (LocalVariable)varAddress,
                Right = (LocalVariable)varValue
            };

            AddOperation(LocalOperation.Kinds.DerefAssignment, assignment);
        }

        #region Branching operators

        public void BranchIfTrue(int pushedIntValue)
        {
            var firstVar = _evaluator.Stack.Pop();
            AddOperation(LocalOperation.Kinds.BranchOperator,
                         new BranchOperator(OpcodeBranchNames.BrTrue)
                         {
                             JumpTo = pushedIntValue,
                             CompareValue = firstVar
                         });
        }

        public void BranchIfFalse(int pushedIntValue)
        {
            var firstVar = _evaluator.Stack.Pop();
            AddOperation(LocalOperation.Kinds.BranchOperator,
                         new BranchOperator(OpcodeBranchNames.BrFalse)
                         {
                             JumpTo = pushedIntValue,
                             CompareValue = firstVar
                         });
        }


        public void AlwaysBranch(int offset)
        {
            AddOperation(LocalOperation.Kinds.AlwaysBranch, offset);
        }

        public void BranchIfEqual(int jumpTo)
        {
            BranchTwoOperators(jumpTo, OpcodeBranchNames.Beq);
        }

        private void BranchTwoOperators(int jumpTo, string opcode)
        {
            var firstVar = _evaluator.Stack.Pop();
            var secondVar = _evaluator.Stack.Pop();

            AddOperation(LocalOperation.Kinds.BranchOperator,
                         new BranchOperator(opcode)
                         {
                             JumpTo = jumpTo,
                             CompareValue = firstVar,
                             SecondValue = secondVar
                         });
        }

        public void BranchIfGreaterOrEqual(int jumpTo)
        {
            BranchTwoOperators(jumpTo, OpcodeBranchNames.Bge);
        }

        public void BranchIfGreater(int jumpTo)
        {
            BranchTwoOperators(jumpTo, OpcodeBranchNames.Bgt);
        }

        public void BranchIfLessOrEqual(int jumpTo)
        {
            BranchTwoOperators(jumpTo, OpcodeBranchNames.Ble);
        }

        public void BranchIfLess(int jumpTo)
        {
            BranchTwoOperators(jumpTo, OpcodeBranchNames.Blt);
        }

        public void BranchIfNotEqual(int jumpTo)
        {
            BranchTwoOperators(jumpTo, OpcodeBranchNames.Bne);
        }

        #endregion

        public void LoadAddressIntoEvaluationStack(LocalVariableInfo index)
        {
            var vreg = SetNewVReg();
            vreg.FixedType = index.LocalType.MakeByRefType();

            var argument = _representation.Vars.LocalVariables[index.LocalIndex];
            var assignment = new RefAssignment
            {
                Left = vreg,
                Right = argument
            };
            AddOperation(LocalOperation.Kinds.RefAssignment, assignment);
        }

        public void LoadField(string fieldName)
        {
            var firstVar = _evaluator.Stack.Pop();

            var vreg = SetNewVReg();
            vreg.FixedType = firstVar.ComputedType().LocateField(fieldName).FieldType;
            ProgramData.UpdateType(vreg.FixedType);
            var assignment = new Assignment
            {
                AssignedTo = vreg,
                Right = new FieldGetter
                {
                    FieldName = fieldName,
                    Instance = firstVar
                }
            };
            AddOperation(LocalOperation.Kinds.GetField, assignment);
        }

        public void LoadStaticField(FieldInfo operand)
        {
            var vreg = SetNewVReg();
            var fieldName = operand.Name;
            var declaringType = operand.DeclaringType;
            vreg.FixedType = declaringType.LocateField(fieldName, true).FieldType;
            var typeData = ProgramData.UpdateType(declaringType);
            var assignment = new Assignment
            {
                AssignedTo = vreg,
                Right = new StaticFieldGetter
                {
                    FieldName = fieldName,
                    DeclaringType = typeData
                }
            };
            AddOperation(LocalOperation.Kinds.GetStaticField, assignment);
        }

        public void NewObject(ConstructorInfo constructorInfo)
        {
            var result = SetNewVReg();

            result.FixedType = constructorInfo.DeclaringType;
            var assignment = new Assignment
            {
                AssignedTo = result,
                Right = new NewConstructedObject(constructorInfo)
            };
            ProgramData.UpdateType(constructorInfo.DeclaringType);
            AddOperation(LocalOperation.Kinds.NewObject, assignment);
        }

        public void NewArray(Type typeArray)
        {
            var arrayLength = _evaluator.Stack.Pop();
            var result = SetNewVReg();
            var assignment = new Assignment
            {
                AssignedTo = result,
                Right = new NewArrayObject
                {
                    TypeArray = typeArray,
                    ArrayLength = arrayLength
                }
            };
            result.FixedType = typeArray.MakeArrayType();
            AddOperation(LocalOperation.Kinds.NewArray, assignment);
        }

        public void SetArrayElementValue()
        {
            var value = _evaluator.Pop();
            var index = _evaluator.Pop();
            var array = _evaluator.Pop();
            var arrayVariable = new ArrayVariable(array, index);
            var assignment = new Assignment
            {
                AssignedTo = arrayVariable,
                Right = value
            };
            arrayVariable.FixedType = value.ComputedType();
            AddOperation(LocalOperation.Kinds.SetArrayItem, assignment);
        }

        public void SetToken(FieldInfo operand)
        {
            var fieldOperand = operand;
            var fieldType = fieldOperand.FieldType;
            var nesterType = fieldType.DeclaringType;
            var fields = nesterType.GetFields(BindingFlags.Static | BindingFlags.NonPublic);
            var value = fields[0].GetValue(null);
            var srcBytes = value.ToByteArray();
            var vreg = SetNewVReg();
            var rightConstant = ConstByteArrayList.RegisterConstant(srcBytes);
            var assign = new Assignment
            {
                AssignedTo = vreg,
                Right = new ConstByteArrayValue(rightConstant)
            };
            vreg.FixedType = typeof(byte[]);
            AddOperation(LocalOperation.Kinds.CopyArrayInitializer, assign);
        }

        public void LoadNull()
        {
            AssignNullToStack();
        }

        private void AssignNullToStack()
        {
            var result = SetNewVReg();
            var assign = new Assignment
            {
                AssignedTo = result,
                Right = new ConstValue(null)
            };
            result.FixedType = typeof(object);
            AddOperation(LocalOperation.Kinds.Assignment, assign);
        }

        public void LoadFunction(MethodBase operand)
        {
            var result = SetNewVReg();
            var store = new FunctionPointerStore()
            {
                AssignedTo = result,
                FunctionPointer = operand
            };

            AddOperation(LocalOperation.Kinds.LoadFunction, store);
        }

        public void Switch(Instruction[] instructions)
        {
            var firstVar = (LocalVariable)_evaluator.Stack.Pop();
            var assign = new Assignment
            {
                AssignedTo = firstVar,
                Right = new ConstValue(instructions)
            };
            AddOperation(LocalOperation.Kinds.Switch, assign);
        }
    }
}