using System;
using System.Text;
using NiceIO;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.Contexts.Services;
using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP;

public static class ErrorMessageWriter
{
	public static Exception FormatException(IErrorInformationService errorInformation, Exception exception)
	{
		return new AdditionalErrorInformationException(FormatErrorMessage(errorInformation, null), exception);
	}

	public static string FormatErrorMessage(IErrorInformationService errorInformation, string additionalInformation)
	{
		return FormatErrorMessage(errorInformation, additionalInformation, GetSequencePoint);
	}

	public static string AppendLocationInformation(IErrorInformationService errorInformationService, string start)
	{
		StringBuilder message = new StringBuilder(start);
		AppendLocationInformation(errorInformationService, message, GetSequencePoint);
		return message.ToString();
	}

	public static string FormatErrorMessage(IErrorInformationService errorInformation, string additionalInformation, Func<Instruction, MethodDefinition, SequencePoint> getSequencePoint)
	{
		if (errorInformation == null)
		{
			throw new ArgumentNullException("errorInformation");
		}
		StringBuilder message = new StringBuilder();
		message.Append("IL2CPP error");
		AppendLocationInformation(errorInformation, message, getSequencePoint);
		if (!string.IsNullOrEmpty(additionalInformation))
		{
			message.AppendLine();
			message.AppendFormat("Additional information: {0}", additionalInformation);
		}
		return message.ToString();
	}

	private static void AppendLocationInformation(IErrorInformationService errorInformation, StringBuilder message, Func<Instruction, MethodDefinition, SequencePoint> getSequencePoint)
	{
		if (errorInformation.CurrentMethod != null)
		{
			message.AppendFormat(" for method '{0}'", errorInformation.CurrentMethod.FullName);
		}
		else if (errorInformation.CurrentField != null)
		{
			message.AppendFormat(" for field '{0}'", errorInformation.CurrentField.FullName);
		}
		else if (errorInformation.CurrentProperty != null)
		{
			message.AppendFormat(" for property '{0}::{1}'", errorInformation.CurrentProperty.DeclaringType.FullName, errorInformation.CurrentProperty.Name);
		}
		else if (errorInformation.CurrentEvent != null)
		{
			message.AppendFormat(" for event '{0}::{1}'", errorInformation.CurrentEvent.DeclaringType.FullName, errorInformation.CurrentEvent.Name);
		}
		else if (errorInformation.CurrentType != null)
		{
			message.AppendFormat(" for type '{0}'", errorInformation.CurrentType);
		}
		else
		{
			message.Append(" (no further information about what managed code was being converted is available)");
		}
		if (!AppendSourceCodeLocation(errorInformation, message, getSequencePoint) && errorInformation.CurrentType != null && errorInformation.CurrentType.Module != null)
		{
			message.AppendFormat(" in assembly '{0}'", errorInformation.CurrentType.Module.FileName ?? errorInformation.CurrentType.Module.Name);
		}
	}

	private static bool AppendSourceCodeLocation(IErrorInformationService errorInformation, StringBuilder message, Func<Instruction, MethodDefinition, SequencePoint> getSequencePoint)
	{
		string sourceCodeLocation = FindSourceCodeLocationForInstruction(errorInformation.CurrentInstruction, errorInformation.CurrentMethod, getSequencePoint);
		if (string.IsNullOrEmpty(sourceCodeLocation))
		{
			sourceCodeLocation = FindSourceCodeLocation(errorInformation.CurrentMethod, getSequencePoint);
		}
		if (string.IsNullOrEmpty(sourceCodeLocation) && errorInformation.CurrentType != null)
		{
			foreach (MethodDefinition method in errorInformation.CurrentType.Methods)
			{
				sourceCodeLocation = FindSourceCodeLocation(method, getSequencePoint);
				if (!string.IsNullOrEmpty(sourceCodeLocation))
				{
					break;
				}
			}
		}
		if (!string.IsNullOrEmpty(sourceCodeLocation))
		{
			message.AppendFormat(" in {0}", sourceCodeLocation);
			return true;
		}
		return false;
	}

	private static string FindSourceCodeLocation(MethodDefinition method, Func<Instruction, MethodDefinition, SequencePoint> getSequencePoint)
	{
		string sourceCodeLocation = string.Empty;
		if (method != null && method.HasBody)
		{
			foreach (Instruction instruction in method.Body.Instructions)
			{
				sourceCodeLocation = FindSourceCodeLocationForInstruction(instruction, method, getSequencePoint);
				if (!string.IsNullOrEmpty(sourceCodeLocation))
				{
					break;
				}
			}
		}
		return sourceCodeLocation;
	}

	private static SequencePoint GetSequencePoint(Instruction ins, MethodDefinition method)
	{
		if (method == null || method.DebugInformation == null || !method.DebugInformation.HasSequencePoints)
		{
			return null;
		}
		return ins?.SequencePoint;
	}

	private static string FindSourceCodeLocationForInstruction(Instruction instruction, MethodDefinition method, Func<Instruction, MethodDefinition, SequencePoint> getSequencePoint)
	{
		if (instruction == null)
		{
			return string.Empty;
		}
		SequencePoint sequencePoint = getSequencePoint(instruction, method);
		if (sequencePoint == null)
		{
			return string.Empty;
		}
		return $"{sequencePoint.Document.Url.ToString(SlashMode.Forward)}:{sequencePoint.StartLine}";
	}
}
