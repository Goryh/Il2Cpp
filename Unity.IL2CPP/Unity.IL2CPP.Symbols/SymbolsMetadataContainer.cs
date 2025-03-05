using System.Collections.Generic;
using System.IO;

namespace Unity.IL2CPP.Symbols;

internal class SymbolsMetadataContainer
{
	public struct LineNumberPair
	{
		public readonly uint CppLineNumber;

		public readonly uint CsLineNumber;

		public LineNumberPair(uint cppLineNumber, uint csLineNumber)
		{
			CppLineNumber = cppLineNumber;
			CsLineNumber = csLineNumber;
		}
	}

	private Dictionary<string, Dictionary<string, List<LineNumberPair>>> m_CPPFilenameToDictionaryOfCSFilenameToLineNumberPairList;

	public SymbolsMetadataContainer()
	{
		m_CPPFilenameToDictionaryOfCSFilenameToLineNumberPairList = new Dictionary<string, Dictionary<string, List<LineNumberPair>>>();
	}

	public void Add(string cppFileName, string csFileName, uint cppLineNum, uint csLineNum)
	{
		List<LineNumberPair> lineNumberPairsList2;
		if (!m_CPPFilenameToDictionaryOfCSFilenameToLineNumberPairList.TryGetValue(cppFileName, out var csFilenameToLineNumberPairsList))
		{
			List<LineNumberPair> lineNumberPairsList = new List<LineNumberPair>();
			lineNumberPairsList.Add(new LineNumberPair(cppLineNum, csLineNum));
			csFilenameToLineNumberPairsList = new Dictionary<string, List<LineNumberPair>>();
			csFilenameToLineNumberPairsList.Add(csFileName, lineNumberPairsList);
			m_CPPFilenameToDictionaryOfCSFilenameToLineNumberPairList.Add(cppFileName, csFilenameToLineNumberPairsList);
		}
		else if (!csFilenameToLineNumberPairsList.TryGetValue(csFileName, out lineNumberPairsList2))
		{
			lineNumberPairsList2 = new List<LineNumberPair>();
			lineNumberPairsList2.Add(new LineNumberPair(cppLineNum, csLineNum));
			csFilenameToLineNumberPairsList.Add(csFileName, lineNumberPairsList2);
		}
		else
		{
			csFilenameToLineNumberPairsList.TryGetValue(csFileName, out lineNumberPairsList2);
			lineNumberPairsList2.Add(new LineNumberPair(cppLineNum, csLineNum));
		}
	}

	public void Merge(SymbolsMetadataContainer forked)
	{
		foreach (KeyValuePair<string, Dictionary<string, List<LineNumberPair>>> item in forked.m_CPPFilenameToDictionaryOfCSFilenameToLineNumberPairList)
		{
			m_CPPFilenameToDictionaryOfCSFilenameToLineNumberPairList[item.Key] = item.Value;
		}
	}

	public void SerializeToJson(StreamWriter outputStream)
	{
		outputStream.WriteLine("{");
		bool firstCppFile = true;
		foreach (KeyValuePair<string, Dictionary<string, List<LineNumberPair>>> cppFile in m_CPPFilenameToDictionaryOfCSFilenameToLineNumberPairList)
		{
			if (firstCppFile)
			{
				firstCppFile = false;
			}
			else
			{
				outputStream.WriteLine(",");
			}
			string cppFileName = cppFile.Key.Replace("\\", "\\\\");
			outputStream.WriteLine("\"" + cppFileName + "\" : {");
			bool firstCsFile = true;
			foreach (KeyValuePair<string, List<LineNumberPair>> csFile in cppFile.Value)
			{
				if (firstCsFile)
				{
					firstCsFile = false;
				}
				else
				{
					outputStream.WriteLine(",");
				}
				string csFileName = csFile.Key.Replace("\\", "\\\\");
				outputStream.WriteLine("\"" + csFileName + "\" : {");
				bool firstLineMapping = true;
				foreach (LineNumberPair lineMapping in csFile.Value)
				{
					if (firstLineMapping)
					{
						firstLineMapping = false;
					}
					else
					{
						outputStream.WriteLine(",");
					}
					outputStream.Write($"\"{lineMapping.CppLineNumber}\" : {lineMapping.CsLineNumber}");
				}
				outputStream.WriteLine();
				outputStream.Write("}");
			}
			outputStream.WriteLine();
			outputStream.Write("}");
		}
		outputStream.WriteLine();
		outputStream.WriteLine("}");
	}
}
