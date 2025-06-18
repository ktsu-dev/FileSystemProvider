// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.FileSystemProvider.Test;

using System;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class FileSystemProviderTests
{
	[TestMethod]
	public void Current_ReturnsDefaultFileSystem_WhenNoFactorySet()
	{
		// Arrange
		FileSystemProvider.ResetToDefault();

		// Act
		IFileSystem result = FileSystemProvider.Current;

		// Assert
		Assert.IsNotNull(result);
		Assert.IsInstanceOfType<FileSystem>(result);
	}

	[TestMethod]
	public void SetFileSystemFactory_ThrowsArgumentNullException_WhenFactoryIsNull()
	{
		// Act & Assert
		Assert.ThrowsException<ArgumentNullException>(() => FileSystemProvider.SetFileSystemFactory(null!));
	}

	[TestMethod]
	public void SetFileSystemFactory_CachesInstanceWithinAsyncContext()
	{
		// Arrange
		FileSystemProvider.SetFileSystemFactory(() => new MockFileSystem());

		// Act
		IFileSystem instance1 = FileSystemProvider.Current;
		IFileSystem instance2 = FileSystemProvider.Current;

		// Assert
		Assert.IsInstanceOfType<MockFileSystem>(instance1);
		Assert.IsInstanceOfType<MockFileSystem>(instance2);
		Assert.AreSame(instance1, instance2, "Factory should cache the same instance within an async context");
	}

	[TestMethod]
	public void ResetToDefault_RestoresDefaultFileSystem_WhenFactoryWasSet()
	{
		// Arrange
		FileSystemProvider.SetFileSystemFactory(() => new MockFileSystem());

		// Act
		FileSystemProvider.ResetToDefault();

		// Assert
		IFileSystem result = FileSystemProvider.Current;
		Assert.IsNotNull(result);
		Assert.IsInstanceOfType<FileSystem>(result);
	}

	[TestMethod]
	public void FileSystemProvider_WithFactory_ProvidesIsolatedInstancesPerAsyncContext()
	{
		// Arrange
		FileSystemProvider.SetFileSystemFactory(() => new MockFileSystem());

		// Act & Assert
		Task<IFileSystem> task1 = Task.Run(() => FileSystemProvider.Current);
		Task<IFileSystem> task2 = Task.Run(() => FileSystemProvider.Current);

		IFileSystem result1 = task1.Result;
		IFileSystem result2 = task2.Result;

		// Each task should get its own instance from the factory
		Assert.IsInstanceOfType<MockFileSystem>(result1);
		Assert.IsInstanceOfType<MockFileSystem>(result2);
		Assert.AreNotSame(result1, result2, "Factory should create isolated instances per async context");
	}

	[TestMethod]
	public void FileSystemProvider_WithFactory_PreservesStateWithinAsyncContext()
	{
		// Arrange
		FileSystemProvider.SetFileSystemFactory(() => new MockFileSystem());

		// Act - Use the filesystem, then get it again
		IFileSystem fs1 = FileSystemProvider.Current;
		fs1.File.WriteAllText("test.txt", "test content");

		IFileSystem fs2 = FileSystemProvider.Current;

		// Assert - Should be the same instance with preserved state
		Assert.AreSame(fs1, fs2, "Should get the same cached instance within async context");
		Assert.IsTrue(fs2.File.Exists("test.txt"), "State should be preserved between calls");
		Assert.AreEqual("test content", fs2.File.ReadAllText("test.txt"), "File content should be preserved");
	}

	[TestMethod]
	public void FileSystemProvider_DefaultInstanceIsLazyInitialized()
	{
		// Arrange
		FileSystemProvider.ResetToDefault();

		// Act
		IFileSystem instance1 = FileSystemProvider.Current;
		IFileSystem instance2 = FileSystemProvider.Current;

		// Assert
		Assert.AreSame(instance1, instance2, "Default instance should be the same lazy-initialized instance");
	}

	[TestMethod]
	public void ResetToDefault_InvalidatesAllAsyncContexts()
	{
		// Arrange - Set up factory and get instances in different contexts
		FileSystemProvider.SetFileSystemFactory(() => new MockFileSystem());

		Task<IFileSystem> task1 = Task.Run(() => FileSystemProvider.Current);
		Task<IFileSystem> task2 = Task.Run(() => FileSystemProvider.Current);

		IFileSystem context1Instance = task1.Result;
		IFileSystem context2Instance = task2.Result;

		// Verify we have mock instances
		Assert.IsInstanceOfType<MockFileSystem>(context1Instance);
		Assert.IsInstanceOfType<MockFileSystem>(context2Instance);

		// Act - Reset to default
		FileSystemProvider.ResetToDefault();

		// Assert - All contexts should now return default filesystem
		Task<IFileSystem> postResetTask1 = Task.Run(() => FileSystemProvider.Current);
		Task<IFileSystem> postResetTask2 = Task.Run(() => FileSystemProvider.Current);

		IFileSystem postReset1 = postResetTask1.Result;
		IFileSystem postReset2 = postResetTask2.Result;

		Assert.IsInstanceOfType<FileSystem>(postReset1);
		Assert.IsInstanceOfType<FileSystem>(postReset2);
		Assert.AreNotSame(context1Instance, postReset1);
		Assert.AreNotSame(context2Instance, postReset2);
	}

	[TestCleanup]
	public void TestCleanup()
	{
		// Ensure each test starts with a clean state
		FileSystemProvider.ResetToDefault();
	}
}
