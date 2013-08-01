﻿using System.Collections.Generic;
using CodeRefractor.RuntimeBase.MiddleEnd;
using CodeRefractor.RuntimeBase.MiddleEnd.SimpleOperations;

namespace CodeRefractor.Compiler.Optimizations.ConstantFoldingAndPropagation.ComplexAssignments
{
    public static class InstructionsUtils
    {
        public static Dictionary<int, int> BuildLabelTable(List<LocalOperation> operations)
        {
            var labelTable = new Dictionary<int, int>();
            labelTable.Clear();
            for (var i = 0; i < operations.Count; i++)
            {
                var operation = operations[i];
                switch (operation.Kind)
                {
                    case LocalOperation.Kinds.Label:
                        var jumpTo = (int)operation.Value;
                        labelTable[jumpTo] = i;
                        break;
                }
            }
            return labelTable;
        }

        public static void DeleteInstructions(this MetaMidRepresentation intermediateCode, HashSet<int> instructionsToBeDeleted)
        {
            var pos = 0;
            var liveOperations = new List<LocalOperation>();
            foreach (var op in intermediateCode.LocalOperations)
            {
                if (!instructionsToBeDeleted.Contains(pos))
                    liveOperations.Add(op);
                pos++;
            }
            intermediateCode.LocalOperations = liveOperations;
            instructionsToBeDeleted.Clear();
        }
    }
}