// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.FileSystemProvider;

using System;

/// <summary>
/// Exception thrown by FileSystemProvider operations
/// </summary>
public class FileSystemProviderException : Exception
{
	/// <summary>
	/// Gets the exception category
	/// </summary>
	public FileSystemProviderExceptionType ExceptionType { get; }

	/// <summary>
	/// Initializes a new instance of the FileSystemProviderException class
	/// </summary>
	public FileSystemProviderException() : base() => ExceptionType = FileSystemProviderExceptionType.InvalidConfiguration;

	/// <summary>
	/// Initializes a new instance of the FileSystemProviderException class
	/// </summary>
	/// <param name="message">The exception message</param>
	public FileSystemProviderException(string message) : base(message) => ExceptionType = FileSystemProviderExceptionType.InvalidConfiguration;

	/// <summary>
	/// Initializes a new instance of the FileSystemProviderException class
	/// </summary>
	/// <param name="message">The exception message</param>
	/// <param name="innerException">The inner exception</param>
	public FileSystemProviderException(string message, Exception innerException) : base(message, innerException) => ExceptionType = FileSystemProviderExceptionType.InvalidConfiguration;

	/// <summary>
	/// Initializes a new instance of the FileSystemProviderException class
	/// </summary>
	/// <param name="type">The type of exception</param>
	/// <param name="message">The exception message</param>
	public FileSystemProviderException(FileSystemProviderExceptionType type, string message) : base(message) =>
		ExceptionType = type;

	/// <summary>
	/// Initializes a new instance of the FileSystemProviderException class
	/// </summary>
	/// <param name="type">The type of exception</param>
	/// <param name="message">The exception message</param>
	/// <param name="innerException">The inner exception</param>
	public FileSystemProviderException(FileSystemProviderExceptionType type, string message, Exception innerException) : base(message, innerException) =>
		ExceptionType = type;
}

/// <summary>
/// Types of FileSystemProvider exceptions
/// </summary>
public enum FileSystemProviderExceptionType
{
	/// <summary>
	/// Factory function returned null
	/// </summary>
	FactoryReturnsNull,

	/// <summary>
	/// Test mode is being used in production environment
	/// </summary>
	TestModeInProduction,

	/// <summary>
	/// Invalid configuration provided
	/// </summary>
	InvalidConfiguration
}
