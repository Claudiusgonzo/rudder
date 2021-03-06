﻿using System;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using Microsoft.Cci;
using Backend;
using System.Xml.Linq;
using ScopeAnalyzer.Misc;
using System.Text;

namespace ScopeAnalyzer
{
    /// <summary>
    /// Struct that saves basic statistics about Scope analysis performance.
    /// </summary>
    public struct ScopeAnalysisStats
    {
        public int Assemblies;
        public int AssembliesLoaded;

        public int Methods;
        public int FailedMethods;
        public int UnsupportedMethods;
        public int InterestingMethods;

        public int NotEscapeDummies;
        public int NotCPropagationDummies;
        public int NotColumnDummies;

        public int Mapped;

        public int UnionColumnsUnused;
        public int UnionColumnsAllUsed;
        public int Warnings;
        public int UnionColumnsSavings;
        public int InputColumnsByteSavings;
        public double UnionColumnsSavingsPercentages;
        public double InputColumnsByteSavingsPercentages;

        public int InputColumnsUnused;
        public int InputColumnsAllUsed;
        public int InputColumnsSavings;
        public double InputColumnsSavingsPercentages;

        public int OutputColumnsUnused;
        public int OutputColumnsAllUsed;
        public int OutputColumnsSavings;
        public double OutputColumnsSavingsPercentages;

        public int ColumnStringAccesses;
        public int ColumnIndexAccesses;

        public ScopeAnalysisStats(int assemblies = 0)
        {
            Assemblies = assemblies;
            AssembliesLoaded = 0;
            Methods = FailedMethods = UnsupportedMethods = InterestingMethods = 0;
            NotEscapeDummies = NotCPropagationDummies = NotColumnDummies = 0;
            Mapped = 0;

            ColumnIndexAccesses = ColumnStringAccesses = 0;

            UnionColumnsUnused = UnionColumnsAllUsed = Warnings = UnionColumnsSavings = 0;
            UnionColumnsSavingsPercentages = 0.0; 

            InputColumnsAllUsed = InputColumnsUnused = InputColumnsSavings = InputColumnsByteSavings = 0; ;
            InputColumnsSavingsPercentages = InputColumnsByteSavingsPercentages = 0.0;

            OutputColumnsAllUsed = OutputColumnsUnused = OutputColumnsSavings = 0;
            OutputColumnsSavingsPercentages = 0.0;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(String.Format("Assemblies: {0}", Assemblies));
            sb.AppendLine(String.Format("Assemblies loaded: {0}", AssembliesLoaded));
            sb.AppendLine();
            sb.AppendLine(String.Format("Methods: {0}", Methods));
            sb.AppendLine(String.Format("Methods failed: {0}", FailedMethods));
            sb.AppendLine();
            sb.AppendLine(String.Format("Interesting methods (not failed): {0}", InterestingMethods));
            sb.AppendLine(String.Format("Unsupported feature methods: {0}", UnsupportedMethods));
            sb.AppendLine();
            sb.AppendLine(String.Format("Concrete-columns-found methods: {0}", NotColumnDummies));
            sb.AppendLine();
            sb.AppendLine(String.Format("Concrete methods successfully mapped: {0}", Mapped));

            sb.AppendLine(String.Format("Union unused: {0}", UnionColumnsUnused));
            sb.AppendLine(String.Format("Union all used: {0}", UnionColumnsAllUsed));
            sb.AppendLine(String.Format("Union superset (warnings): {0}", Warnings));
            sb.AppendLine();           
            sb.AppendLine(String.Format("Union columns average count savings: {0}", (UnionColumnsUnused == 0 ? 0 : UnionColumnsSavings / (double)UnionColumnsUnused)));          
            sb.AppendLine(String.Format("Union columns average count percentage savings: {0}", (UnionColumnsUnused == 0? 0: UnionColumnsSavingsPercentages/(double) UnionColumnsUnused)));        
            //sb.AppendLine(String.Format("Union columns average byte savings: {0}", (UnionColumnsUnused == 0 ? 0 : InputColumnsByteSavings / (double)UnionColumnsUnused)));          
            //sb.AppendLine(String.Format("Union columns average byte percentage savings: {0}", (UnionColumnsUnused == 0 ? 0 : InputColumnsByteSavingsPercentages / (double)UnionColumnsUnused)));
            sb.AppendLine();

            sb.AppendLine(String.Format("Input unused: {0}", InputColumnsUnused));
            sb.AppendLine(String.Format("Input all used: {0}", InputColumnsAllUsed));
            sb.AppendLine();
            sb.AppendLine(String.Format("Input columns average count savings: {0}", (InputColumnsUnused == 0 ? 0 : InputColumnsSavings / (double)InputColumnsUnused)));
            sb.AppendLine(String.Format("Input columns average count percentage savings: {0}", (InputColumnsUnused == 0 ? 0 : InputColumnsSavingsPercentages / (double)InputColumnsUnused)));
            sb.AppendLine();

            sb.AppendLine(String.Format("Output unused: {0}", OutputColumnsUnused));
            sb.AppendLine(String.Format("Output all used: {0}", OutputColumnsAllUsed));
            sb.AppendLine();
            sb.AppendLine(String.Format("Output columns average count savings: {0}", (OutputColumnsUnused == 0 ? 0 : OutputColumnsSavings / (double)OutputColumnsUnused)));
            sb.AppendLine(String.Format("Output columns average count percentage savings: {0}", (OutputColumnsUnused == 0 ? 0 : OutputColumnsSavingsPercentages / (double)OutputColumnsUnused)));
            sb.AppendLine();


            sb.AppendLine(String.Format("Used columns string accesses: {0}", ColumnStringAccesses));
            sb.AppendLine(String.Format("Used columns index accesses: {0}", ColumnIndexAccesses));
            sb.AppendLine();
            sb.AppendLine();

            sb.AppendLine(String.Format("!Union columns count cumulative savings: {0}", UnionColumnsSavings));
            sb.AppendLine(String.Format("!Union columns percentage count cumulative savings: {0}", UnionColumnsSavingsPercentages));
            //sb.AppendLine(String.Format("!Union columns byte cumulative savings: {0}", InputColumnsByteSavings));
            //sb.AppendLine(String.Format("!Union columns percentage byte cumulative savings: {0}", InputColumnsByteSavingsPercentages));
            sb.AppendLine();

            sb.AppendLine(String.Format("!Input columns count cumulative savings: {0}", InputColumnsSavings));
            sb.AppendLine(String.Format("!Input columns percentage count cumulative savings: {0}", InputColumnsSavingsPercentages));
            sb.AppendLine();

            sb.AppendLine(String.Format("!Output columns count cumulative savings: {0}", OutputColumnsSavings));
            sb.AppendLine(String.Format("!Output columns percentage count cumulative savings: {0}", OutputColumnsSavingsPercentages));

            return sb.ToString();
        }
    }



    public static class Program
    {
        public static void Main(string[] args)
        {

            var r = GetUsedColumns(args[0], args[1]);

            Options options = Options.ParseCommandLineArguments(args);

            if (options.AskingForHelp)
            {
                Utils.WriteLine("Check README file in the project root.");
                return;
            }
            if (options.BreakIntoDebugger)
            {
                if (Debugger.IsAttached)
                {
                    Debugger.Break();
                }
                else
                {
                    Debugger.Launch();
                }
            }


            if (options.OutputPath != null) Utils.SetOutput(options.OutputPath);
            Utils.WriteLine("Parsed input arguments, starting the analysis...");

           
            var stats = AnalyzeAssemblies(options);
          

            Utils.IsVerbose = true;
            Utils.WriteLine(stats.ToString());
            Utils.WriteLine("SUCCESS");
            Utils.OutputClose();
        }


        public static ScopeAnalysisStats AnalyzeAssemblies(Options options)
        {
            Utils.IsVerbose = options.Verbose;

            var vertexDef = LoadVertexDef(options);
            var processorIdMapping = LoadProcessorMapping(options);
           
            var host = new PeReader.DefaultHost();
            var assemblies = LoadAssemblies(host, options.ReferenceAssemblies, options.Assemblies);
            var stats = new ScopeAnalysisStats();
            stats.Assemblies = options.Assemblies.Count;

            var mainAssemblies = assemblies.Item1;
            stats.AssembliesLoaded = mainAssemblies.Count;
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            foreach (var mAssembly in mainAssemblies)
            {
                try
                {
                    Utils.WriteLine("\n====== Analyzing assembly: " + mAssembly.Name + " =========");

                    // If processor to id mapping and xml with id information are both available, 
                    // then we ask ScopeAnalysis to analyze only those processors mentioned in the mapping.
                    var results = AnalyzeAssembly(host, mAssembly, null, assemblies.Item2, (processorIdMapping == null || vertexDef == null) ? null : processorIdMapping.Keys);

                    //Update the stats.
                    UpdateStats(results, ref stats, vertexDef, processorIdMapping);

                    Utils.WriteLine("\n====== Done analyzing the assembly  =========");
                }
                catch (ScopeAnalysis.MissingScopeMetadataException e)
                {
                    Utils.WriteLine("ASSEMBLY WARNING: " + e.Message);
                }
                catch (Exception e)
                {
                    Utils.WriteLine("ASSEMBLY FAILURE: " + e.Message);
                }
            }
            
            stopWatch.Stop();
            var ts = stopWatch.Elapsed;
            Utils.WriteLine(String.Format("Total analysis time: {0:00}:{1:00}:{2:00}.{3:00}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10));

            return stats;
        }

        private static IEnumerable<ScopeMethodAnalysisResult> AnalyzeAssembly(IMetadataHost host, IAssembly assembly, ISourceLocationProvider sourceLocationProvider, IEnumerable<IAssembly> referenceAssemblies, IEnumerable<string> ips)
        {
            if (ips == null)
            {
                Utils.WriteLine("Interesting processors list not provided, continuing without it.");
            }

            var analysis = new ScopeAnalysis(host, assembly, sourceLocationProvider, referenceAssemblies, ips);
            analysis.Analyze();
            return analysis.Results;
        }

        private static IEnumerable<Tuple<string, IEnumerable<string>>> GetUsedColumns(IEnumerable<ScopeMethodAnalysisResult> results)
        {
            var interestingResults = results.Where(r => !r.Failed && r.Interesting).ToList();
            var concreteResults = interestingResults.Where(r => !r.UsedColumnsSummary.IsTop && !r.UsedColumnsSummary.IsBottom);
            return concreteResults.Select(r => Tuple.Create(r.ProcessorType.Name(), r.UsedColumnsSummary.Elements.Select(e => e.ToString())));
        }

        public static IEnumerable<Tuple<string, IEnumerable<string>>> GetUsedColumns(string assemblyPath, string libPath)
        {
            try
            {
                var host = new PeReader.DefaultHost();
                var referenceAssemblies = Utils.CollectAssemblies(libPath);
                var assemblies = LoadAssemblies(host, referenceAssemblies, new string[] { assemblyPath, });
                var mainAssemblies = assemblies.Item1;
                var a = mainAssemblies[0];
                var results = AnalyzeAssembly(host, a, null, assemblies.Item2, null);
                return GetUsedColumns(results);
            }
            catch { return Enumerable<Tuple<string, IEnumerable<string>>>.Empty; }
        }





        //static Dictionary<string, int> TYPE_SIZES = new Dictionary<string, int>() { {"int", 4}, {"int?", 8}, {"float", 4}, {"float?", 8}, {"double", 8}, {"double?", 12},
        //    {"long", 8}, {"long?", 12}, {"DateTime", 8}, {"DateTime?", 12}, {"char", 2}, {"char?", 4}, {"string", 20}, {"string?", 20}, {"binary", 20 }, {"binary?", 20},
        //    {"Guid", 16}, {"Guid?", 20} };

        //static int DEFAULT_TYPE_SIZE = 20;

        private static void ComputeImprovementStats(ScopeMethodAnalysisResult result, ref ScopeAnalysisStats stats, 
                                                    XElement vDef, Dictionary<string, string> pIdMapping)
        {
            stats.ColumnIndexAccesses += result.ColumnIndexAccesses;
            stats.ColumnStringAccesses += result.ColumnStringAccesses;

            if (vDef == null || pIdMapping == null)
                return;

            var column = result.UsedColumnsSummary;
            if (column.IsBottom || column.IsTop) return;          

            var pTypeFullName = result.ProcessorType.FullName();
            Utils.WriteLine("Checking column usage for " + pTypeFullName);

            if (!pIdMapping.ContainsKey(pTypeFullName))
            {
                Utils.WriteLine("WARNING: could not match processor mapping: " + pTypeFullName);
                return;
            }

            stats.Mapped += 1;
            try
            {
                var id = pIdMapping[pTypeFullName];
                var operators = vDef.Descendants("operator");
                // Id can appear several times in the xml file since the same reducer can be used multiple times
                // and contained within different Scope vertices.
                var process = operators.Where(op => op.Attribute("id") != null && op.Attribute("id").Value.Equals(id)).ToList().First();

                // TODO: make parsing take into account commas in generics. Current approach
                // does not invalidate results, but is not clean.
                var input_schema = process.Descendants("input").Single().Attribute("schema").Value.Split(',');               
                var inputSchema = new Dictionary<string, string>();
                foreach (var input in input_schema)
                {                    
                    if (!input.Contains(":")) continue;
                    var parts = input.Split(':');
                    var name = parts[0].Trim();
                    var type = parts[1].Trim();
                    inputSchema[name] = type;
                }
                var inputColumns = inputSchema.Keys.ToList();

                // TODO: make parsing take into account commas in generics. Current approach
                // does not invalidate results, but is not clean.
                var output_schema = process.Descendants("output").Single().Attribute("schema").Value.Split(',');
                var outputSchema = new Dictionary<string, string>();
                foreach (var output in output_schema)
                {                
                    if (!output.Contains(":")) continue;
                    var parts = output.Split(':');
                    var name = parts[0].Trim();
                    var type = parts[1].Trim();
                    outputSchema[name] = type;
                }
                var outputColumns = outputSchema.Keys.ToList();


                var usedColumns = new HashSet<string>();
                foreach (var c in column.Elements)
                {
                    var val = c.Value;
                    if (val is string)
                    {
                        usedColumns.Add(val as string);
                    }
                    else if (val is int)
                    {
                        int index = Int32.Parse(val.ToString());
                        if (index >= 0 && index < inputColumns.Count)
                            usedColumns.Add(inputColumns[index]);
                        if (index >= 0 && index < outputColumns.Count)
                            usedColumns.Add(outputColumns[index]);

                        if ((index >= inputColumns.Count && index >= outputColumns.Count) || index < 0)
                            Utils.WriteLine("WARNING: some index was out of schema range: " + index);
                    }
                    else
                    {
                        Utils.WriteLine("WARNING: other value type used for indexing besides string and int: " + val);
                        return;
                    }
                }


                // Compute stats for schema input-output union.
                var allSchemaColumns = new HashSet<string>(inputColumns.Union(outputColumns));
                var redundants = allSchemaColumns.Except(usedColumns);
                if (redundants.Any())
                {
                    stats.UnionColumnsUnused += 1;
                   
                    var savings = redundants.Count();

                    stats.UnionColumnsSavings += savings;
                    stats.UnionColumnsSavingsPercentages += savings / (double)allSchemaColumns.Count;

                    Utils.WriteLine(String.Format("SAVINGS (union) ({0}): used union columns subset of defined columns: {1}", result.Method.FullName(), savings));
                }
                else
                {
                    stats.UnionColumnsAllUsed += 1;
                    Utils.WriteLine("ALL USED (union): all union columns used.");
                }

                if (allSchemaColumns.IsProperSubsetOf(usedColumns))
                {
                    Utils.WriteLine("OVERAPPROXIMATION: redundant used columns: " + String.Join(" ", usedColumns.Except(allSchemaColumns)));
                    stats.Warnings += 1;
                }

                // Compute stats for input schema.
                redundants = inputColumns.Except(usedColumns);
                if (redundants.Any())
                {
                    stats.InputColumnsUnused += 1;

                    var savings = redundants.Count();

                    stats.InputColumnsSavings += savings;
                    stats.InputColumnsSavingsPercentages += savings / (double)inputColumns.Count;

                    //var redundantInputByteSize = ComputeColumnsSize(redundants.Except(outputColumns), inputSchema);
                    //var inputByteSize = ComputeColumnsSize(inputColumns, inputSchema);

                    //stats.InputColumnsByteSavings += redundantInputByteSize;
                    //stats.InputColumnsByteSavingsPercentages += redundantInputByteSize / (double)inputByteSize;

                    Utils.WriteLine(String.Format("SAVINGS (input) ({0}): used input columns subset of defined columns: {1}", result.Method.FullName(), savings));
                }
                else
                {
                    stats.InputColumnsAllUsed += 1;
                    Utils.WriteLine("All USED (input): all input columns used.");
                }

                // Compute stats for input schema.
                redundants = outputColumns.Except(usedColumns);
                if (redundants.Any())
                {
                    stats.OutputColumnsUnused += 1;

                    var savings = redundants.Count();

                    stats.OutputColumnsSavings += savings;
                    stats.OutputColumnsSavingsPercentages += savings / (double)outputColumns.Count;

                    Utils.WriteLine(String.Format("SAVINGS (output) ({0}): used output columns subset of defined columns: {1}", result.Method.FullName(), savings));
                }
                else
                {
                    stats.OutputColumnsAllUsed += 1;
                    Utils.WriteLine("All USED (output): all output columns used.");
                }

            } 
            catch (Exception e)
            {
                Utils.WriteLine(String.Format("ERROR: failed to compute column usage for {0} {1}", pTypeFullName, e.Message));
            }
        }


        //private static int ComputeColumnsSize(IEnumerable<string> columns, Dictionary<string, string> schema)
        //{
        //    int size = 0;
        //    foreach(var column in columns)
        //    {
        //        var type = schema[column];
        //        if (TYPE_SIZES.ContainsKey(type))
        //        {
        //            size += TYPE_SIZES[type];
        //        }
        //        else
        //        {
        //            size += DEFAULT_TYPE_SIZE;
        //        }
        //    }
        //    return size;
        //}




        private static void UpdateStats(IEnumerable<ScopeMethodAnalysisResult> results, ref ScopeAnalysisStats stats,
                                     XElement vDef, Dictionary<string, string> pIdMapping)
        {
            stats.Methods += results.Count();
            stats.FailedMethods += results.Where(r => r.Failed).ToList().Count;

            var interestingResults = results.Where(r => !r.Failed && r.Interesting).ToList();
            stats.InterestingMethods += interestingResults.Count;
            stats.UnsupportedMethods += interestingResults.Where(r => r.Unsupported).ToList().Count;

            stats.NotEscapeDummies += interestingResults.Where(r => !r.EscapeSummary.IsTop).ToList().Count;
            stats.NotCPropagationDummies += interestingResults.Where(r => r.CPropagationSummary != null && !r.CPropagationSummary.IsTop && !r.CPropagationSummary.IsBottom).ToList().Count;

            var concreteResults = interestingResults.Where(r => !r.UsedColumnsSummary.IsTop && !r.UsedColumnsSummary.IsBottom).ToList();
            stats.NotColumnDummies += concreteResults.Count;

            foreach (var result in concreteResults)
            {
                ComputeImprovementStats(result, ref stats, vDef, pIdMapping);
            }
        }



        [HandleProcessCorruptedStateExceptions]
        public static Tuple<List<IAssembly>, List<IAssembly>> LoadAssemblies(IMetadataHost host, IEnumerable<string> referenceAssemblies, IEnumerable<string> assemblyNames)
        {
            // First, load all the reference assemblies.
            var refs = new List<IAssembly>();
            foreach (var rassembly in referenceAssemblies)
            {
                try
                {
                    //TODO: is this a CCI bug?
                    if (rassembly.EndsWith("__ScopeCodeGen__.dll")) continue;

                    var rasm = host.LoadUnitFrom(rassembly) as IAssembly;
                    refs.Add(rasm);
                    Utils.WriteLine("Successfully loaded reference assembly: " + rassembly);
                }
                catch (AccessViolationException e)
                {
                    Utils.WriteLine("Warning: perhaps this is a library with unmanaged code?");
                }

                catch (Exception e)
                {
                    Utils.WriteLine(String.Format("Warning: failed to load reference assembly {0} ({1})", rassembly, e.Message));
                }
            }

            // Now, load the main assemblies.
            var assemblies = new List<IAssembly>();
            foreach (var assembly in assemblyNames)
            {
                try
                {
                    var asm = host.LoadUnitFrom(assembly) as IAssembly;
                    assemblies.Add(asm);
                    Utils.WriteLine("Successfully loaded main assembly: " + assembly);
                }
                catch (AccessViolationException e)
                {
                    Utils.WriteLine("Warning: perhaps this is a library with unmanaged code?");
                }
                catch (Exception e)
                {
                    Utils.WriteLine(String.Format("LOAD FAILURE: failed to load main assembly {0} ({1})", assembly, e.Message));
                }
            }

            Types.Initialize(host);

            return Tuple.Create(assemblies, refs);
        }

        private static XElement LoadVertexDef(Options options)
        {
            if (File.Exists(options.VertexDefPath))
            {
                try
                {
                    return XElement.Load(options.VertexDefPath);
                }
                catch { }
            }

            Utils.WriteLine(String.Format("WARNING: could not properly load vertex def: {0}", options.VertexDefPath));
            return null;
        }

        private static Dictionary<string, string> LoadProcessorMapping(Options options)
        {
            if (options.ProcessorIdPath == null)
            {
                Utils.WriteLine("No processor to id mapping file available");
                return null;
            }
                
            if (File.Exists(options.ProcessorIdPath))
            {
                try
                {
                    var lines = File.ReadAllLines(options.ProcessorIdPath);
                    var mapping = new Dictionary<string, string>();
                    foreach(var line in lines)
                    {
                        var pair = line.Trim().Split('\t');
                        if (pair.Length != 2)
                            throw new Exception("Processor to id mapping not in correct format!");
                        mapping.Add(pair[0].Trim(), pair[1].Trim());
                    }
                    return mapping;
                }
                catch { }
            }

            Utils.WriteLine(String.Format("WARNING: could not properly load processor to id mapping: {0}", options.ProcessorIdPath));
            return null;
        }
    }
}
