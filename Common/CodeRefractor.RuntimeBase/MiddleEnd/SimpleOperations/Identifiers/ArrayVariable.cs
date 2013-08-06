#region Usings

using System;

#endregion

namespace CodeRefractor.RuntimeBase.MiddleEnd.SimpleOperations.Identifiers
{
    public class ArrayVariable : LocalVariable
    {
        public IdentifierValue Index { get; set; }
        public IdentifierValue Parent;

        public ArrayVariable(IdentifierValue parent, IdentifierValue id)
        {
            Id = -1;
            Kind = VariableKind.Local;
            Index = id;
            Parent = parent;
        }

        public Type GetElementType()
        {
            var computedType = ComputedType();
            var elementType = computedType.GetElementType();
            return elementType;
        }

        public override Type ComputedType()
        {
            return Parent.ComputedType();
        }
    }
}