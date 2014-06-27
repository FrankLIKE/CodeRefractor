using CodeRefractor.MiddleEnd.SimpleOperations.Identifiers;
using CodeRefractor.RuntimeBase.Analyze;

namespace CodeRefractor.MiddleEnd.SimpleOperations
{
    public class SetField : LocalOperation
    {
        public SetField()
            : base(OperationKind.SetField)
        {
        }

        public IdentifierValue Instance { get; set; }
        public string FieldName { get; set; }

        public IdentifierValue Right { get; set; }
        public TypeDescription FixedType { get; set; }

        public override string ToString()
        {
            return string.Format("{0}.{1}= {2}", Instance.Name, FieldName, Right.Name);
        }
    }
}