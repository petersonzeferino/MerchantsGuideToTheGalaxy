﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MerchantsGuideToTheGalaxy.Core.CommandProcessor.Commands;
using MerchantsGuideToTheGalaxy.Core.CommandProcessor.Symbols;
using MerchantsGuideToTheGalaxy.Core.Roman;

namespace MerchantsGuideToTheGalaxy.Core.CommandProcessor
{
    public class Processor
    {
        public Dictionary<ConstantSymbol, RomanSymbol> ConstantsTable { get; private set; }
        public Dictionary<ClassifierSymbol, List<UnitSymbol>> ClassifiersTable { get; private set; }

        public Processor()
        {
            ConstantsTable = new Dictionary<ConstantSymbol, RomanSymbol>();
            ClassifiersTable = new Dictionary<ClassifierSymbol, List<UnitSymbol>>();
        }

        public CommandResult Execute(ConstantDeclarationCommand declaration)
        {
            var constantSymbol = (ConstantSymbol) declaration.Symbols.Single(s => s is ConstantSymbol);            
            
            if (ConstantsTable.ContainsKey(constantSymbol))
                throw new DuplicatedDeclarationException();

            var romanSymbol = (RomanSymbol)declaration.Symbols.Single(s => s is RomanSymbol);

            ConstantsTable.Add(constantSymbol, romanSymbol);

            return new CommandResult
            {
                ResultText = String.Format("Information Registred: \"{0}\"", declaration),
                Sucess = true
            };
        }

        public CommandResult Execute(ClassifierDeclarationCommand declaration)
        {
            var classifier = (ClassifierSymbol)declaration.Symbols.Single(s => s is ClassifierSymbol);
            var unit = (UnitSymbol)declaration.Symbols.Single(s => s is UnitSymbol);
            var value = declaration.Symbols.Single(s => s.Kind == SymbolKind.ValueDefinition).ToDouble();

            unit.Factor = CalculateUnitFactor(declaration.Symbols.OfType<ConstantSymbol>(), value);

            if (!ClassifiersTable.ContainsKey(classifier ))
                ClassifiersTable.Add(classifier, new List<UnitSymbol>());

            if (!ClassifiersTable[classifier].Contains(unit))
                ClassifiersTable[classifier].Add(unit);
            else
                throw new DuplicatedDeclarationException();

            return new CommandResult
            {
                ResultText = String.Format("Information Registred: \"{0}\"", declaration),
                Sucess = true
            };
        }

        public CommandResult Execute(QueryCommand query)
        {
            var queryType = query.Symbols.Single(s => s.Kind == SymbolKind.SubStatemant).Name;

            string messageText;

            var constants = query.Symbols.OfType<ConstantSymbol>().ToList();
            var value = GetDecimalValue(constants);
            var constantsName = string.Join(" ",constants.Select(c => c.ToString()));

            if (queryType == Keywords.SubStatements.Much)
                messageText = string.Format("{0} is {1}", constantsName, value);

            else
            {
                var classifier = (ClassifierSymbol) query.Symbols.Single(s => s is ClassifierSymbol);
                var unit = (UnitSymbol) query.Symbols.Single(s => s is UnitSymbol);

                value *= ClassifiersTable[classifier].Find(u => u.Equals(unit)).Factor;

                messageText = string.Format("{0} {1} is {2} {3}", constantsName, classifier, value, unit);
            }

            return new CommandResult
            {
                ResultText = messageText,
                Sucess = true
            };

        }

        private double GetDecimalValue(IEnumerable<ConstantSymbol> constants)
        {
            var romanSymbols = new StringBuilder();

            constants.Select(c => ConstantsTable[c])
                     .ToList().ForEach(r => romanSymbols.Append(r));

            var romanNumber = romanSymbols.ToString();

            return RomanToDecimalConverter.Convert(romanNumber);
        }
        
        public double CalculateUnitFactor(IEnumerable<ConstantSymbol> constants, double value)
        {
            return value / GetDecimalValue(constants);
        }
    }
}
