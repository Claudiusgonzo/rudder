﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Backend.ThreeAddressCode.Values;
using Backend.ThreeAddressCode.Instructions;
using Backend.Analysis;
using Backend.Visitors;
using Microsoft.Cci;

namespace ScopeAnalyzer
{
    public class ColumnsDomain : SetDomain<Constant>
    {
        private ColumnsDomain(List<Constant> columns)
        {
            elements = columns;
        }

        public static ColumnsDomain Top
        {
            get { return new ColumnsDomain(null); }
        }

        public static ColumnsDomain Bottom
        {
            get { return new ColumnsDomain(new List<Constant>()); }
        }

        public void SetAllColumns()
        {
            base.SetTop();
        }

        public ColumnsDomain Clone()
        {
            var ncols = elements == null ? null : new List<Constant>(elements);
            return new ColumnsDomain(ncols);
        }

        public override string ToString()
        {
            if (IsTop) return "All columns used.";
            if (IsBottom) return "Column information unknown.";
            string summary = String.Empty;
            foreach(var el in elements)
            {
                summary += el.ToString() + "\n";
            }
            return summary;
        }
    }

    /// <summary>
    /// Analysis assumes no rows can escape. If some rows can indeed escape,
    /// then this analysis should not be used.
    /// </summary>
    class UsedColumnsAnalysis
    {
        ControlFlowGraph cfg;
        ConstantsInfoProvider constInfo;
        List<ITypeDefinition> rowTypes;
        List<ITypeDefinition> columnTypes;
        IMetadataHost host;
        bool unsupported = false;

        private HashSet<string> trustedRowMethods = new HashSet<string>() { "get_Item", "get_Schema" };

        public UsedColumnsAnalysis(IMetadataHost h, ControlFlowGraph c, ConstantsInfoProvider ci, List<ITypeDefinition> r, List<ITypeDefinition> cd)
        {
            host = h;
            cfg = c;
            constInfo = ci;
            rowTypes = r;
            columnTypes = cd;

            Initialize();
        }

        private void Initialize()
        {
            var instructions = new List<Instruction>();
            foreach (var block in cfg.Nodes)
                instructions.AddRange(block.Instructions);

            if (instructions.Any(i => i is ThrowInstruction || i is CatchInstruction))
                unsupported = true;
        }



        public IMetadataHost Host
        {
            get { return host; }
        }

        public bool Unsupported
        {
            get { return unsupported; }
        }



        public ColumnsDomain Analyze()
        {
            if (unsupported)
                return ColumnsDomain.Top;


            var cd = ColumnsDomain.Bottom;

            foreach(var node in cfg.Nodes)
            {
                foreach(Instruction instruction in node.Instructions)
                {
                    if (!(instruction is MethodCallInstruction || instruction is IndirectMethodCallInstruction)) continue;

                    if (instruction is MethodCallInstruction)
                    {
                        var ins = instruction as MethodCallInstruction;
                        cd.Join(GetCols(ins, false, ins.Method.Name.Value, ins.Arguments));
                    }
                    else
                    {
                        var ins = instruction as IndirectMethodCallInstruction;
                        cd.Join(GetCols(ins, true, null, ins.Arguments));
                    }

                    // This is a doomed point, no point in continuing the analysis.
                    if (cd.IsTop)
                        return cd;
                }
            }

            return cd;
        }


        /// <summary>
        /// If the caller is a row, then we only accept get_Item method.
        /// </summary>
        /// <param name="arguments"></param>
        /// <returns></returns>
        private ColumnsDomain GetCols(Instruction instruction, bool isVirtual, string name, IList<IVariable> arguments)
        {
            var _this = arguments.ElementAt(0);

            // The methods must belong to Row.
            if (rowTypes.All(rt => _this.Type != null && !_this.Type.SubtypeOf(rt, host))) return ColumnsDomain.Bottom;

            if (isVirtual)
            {
                return ColumnsDomain.Top;
            }
            else
            {
                if (!trustedRowMethods.Contains(name))
                    return ColumnsDomain.Top;

                if (arguments.Count == 1)
                {
                    return ColumnsDomain.Bottom;
                }

                var arg = arguments.ElementAt(1);
                var cons = constInfo.GetConstants(instruction, arg);
                if (cons == null)
                {
                    return ColumnsDomain.Top;
                }
                else
                {
                    var cols = ColumnsDomain.Bottom;
                    foreach (var c in cons) cols.Add(c);
                    return cols;
                }             
            }
        }


        //private bool IsResultColumn(IVariable result)
        //{
        //    return columnTypes.Any(ct => result.Type.IncludesType(ct, host));
        //}
    }
}
