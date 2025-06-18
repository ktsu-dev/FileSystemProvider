// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.FileSystemProvider;

/// <summary>
/// Configuration options for FileSystemProvider
/// </summary>
public class FileSystemProviderOptions
{

	/// <summary>
	/// Gets or sets whether to throw an exception when test mode is used in production
	/// </summary>
	public bool ThrowOnTestModeInProduction { get; set; } = true;
}
