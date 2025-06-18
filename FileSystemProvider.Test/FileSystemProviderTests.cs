// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.FileSystemProvider.Test;

using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class FileSystemProviderTests
{
	private static FileSystemProviderOptions TestOptions => new()
	{
		ThrowOnTestModeInProduction = false
	};

	[TestMethod]
	public void Current_ReturnsDefaultFileSystem_WhenNoFactorySet()
	{
		// Arrange
		FileSystemProvider provider = new(TestOptions);

		// Act
		IFileSystem result = provider.Current;

		// Assert
		Assert.IsNotNull(result);
		Assert.IsInstanceOfType<FileSystem>(result);
	}

	[TestMethod]
	public void SetFileSystemFactory_ThrowsArgumentNullException_WhenFactoryIsNull()
	{
		// Arrange
		FileSystemProvider provider = new(TestOptions);

		// Act & Assert
		Assert.ThrowsException<ArgumentNullException>(() => provider.SetFileSystemFactory(null!));
	}

	[TestMethod]
	public void SetFileSystemFactory_CachesInstanceWithinAsyncContext()
	{
		// Arrange
		FileSystemProvider provider = new(TestOptions);
		provider.SetFileSystemFactory(() => new MockFileSystem());

		// Act
		IFileSystem instance1 = provider.Current;
		IFileSystem instance2 = provider.Current;

		// Assert
		Assert.IsInstanceOfType<MockFileSystem>(instance1);
		Assert.IsInstanceOfType<MockFileSystem>(instance2);
		Assert.AreSame(instance1, instance2, "Factory should cache the same instance within an async context");
	}

	[TestMethod]
	public void ResetToDefault_RestoresDefaultFileSystem_WhenFactoryWasSet()
	{
		// Arrange
		FileSystemProvider provider = new(TestOptions);
		provider.SetFileSystemFactory(() => new MockFileSystem());

		// Act
		provider.ResetToDefault();

		// Assert
		IFileSystem result = provider.Current;
		Assert.IsNotNull(result);
		Assert.IsInstanceOfType<FileSystem>(result);
	}

	[TestMethod]
	public void FileSystemProvider_WithFactory_ProvidesIsolatedInstancesPerAsyncContext()
	{
		// Arrange
		FileSystemProvider provider = new(TestOptions);
		provider.SetFileSystemFactory(() => new MockFileSystem());

		// Act & Assert
		Task<IFileSystem> task1 = Task.Run(() => provider.Current);
		Task<IFileSystem> task2 = Task.Run(() => provider.Current);

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
		FileSystemProvider provider = new(TestOptions);
		provider.SetFileSystemFactory(() => new MockFileSystem());

		// Act - Use the filesystem, then get it again
		IFileSystem fs1 = provider.Current;
		fs1.File.WriteAllText("test.txt", "test content");

		IFileSystem fs2 = provider.Current;

		// Assert - Should be the same instance with preserved state
		Assert.AreSame(fs1, fs2, "Should get the same cached instance within async context");
		Assert.IsTrue(fs2.File.Exists("test.txt"), "State should be preserved between calls");
		Assert.AreEqual("test content", fs2.File.ReadAllText("test.txt"), "File content should be preserved");
	}

	[TestMethod]
	public void FileSystemProvider_DefaultInstanceIsLazyInitialized()
	{
		// Arrange
		FileSystemProvider provider = new(TestOptions);

		// Act
		IFileSystem instance1 = provider.Current;
		IFileSystem instance2 = provider.Current;

		// Assert
		Assert.AreSame(instance1, instance2, "Default instance should be the same lazy-initialized instance");
	}

	[TestMethod]
	public void ResetToDefault_InvalidatesAllAsyncContexts()
	{
		// Arrange - Set up factory and get instances in different contexts
		FileSystemProvider provider = new(TestOptions);
		provider.SetFileSystemFactory(() => new MockFileSystem());

		Task<IFileSystem> task1 = Task.Run(() => provider.Current);
		Task<IFileSystem> task2 = Task.Run(() => provider.Current);

		IFileSystem context1Instance = task1.Result;
		IFileSystem context2Instance = task2.Result;

		// Verify we have mock instances
		Assert.IsInstanceOfType<MockFileSystem>(context1Instance);
		Assert.IsInstanceOfType<MockFileSystem>(context2Instance);

		// Act - Reset to default
		provider.ResetToDefault();

		// Assert - All contexts should now return default filesystem
		Task<IFileSystem> postResetTask1 = Task.Run(() => provider.Current);
		Task<IFileSystem> postResetTask2 = Task.Run(() => provider.Current);

		IFileSystem postReset1 = postResetTask1.Result;
		IFileSystem postReset2 = postResetTask2.Result;

		Assert.IsInstanceOfType<FileSystem>(postReset1);
		Assert.IsInstanceOfType<FileSystem>(postReset2);
		Assert.AreNotSame(context1Instance, postReset1);
		Assert.AreNotSame(context2Instance, postReset2);
	}

	[TestMethod]
	public void AddFileSystemProvider_RegistersServices_Successfully()
	{
		// Arrange
		ServiceCollection services = new();

		// Act
		services.AddFileSystemProvider();
		using ServiceProvider serviceProvider = services.BuildServiceProvider();

		// Assert
		IFileSystemProvider provider = serviceProvider.GetRequiredService<IFileSystemProvider>();
		Assert.IsNotNull(provider);
		Assert.IsInstanceOfType<FileSystemProvider>(provider);
	}

	[TestMethod]
	public void AddFileSystemProvider_WithCustomFactory_RegistersServices_Successfully()
	{
		// Arrange
		ServiceCollection services = new();
		FileSystemProvider customProvider = new();

		// Act
		services.AddFileSystemProvider(_ => customProvider);
		using ServiceProvider serviceProvider = services.BuildServiceProvider();

		// Assert
		IFileSystemProvider provider = serviceProvider.GetRequiredService<IFileSystemProvider>();
		Assert.IsNotNull(provider);
		Assert.AreSame(customProvider, provider);
	}

	[TestMethod]
	public void FileSystemProvider_IsRegisteredAsSingleton()
	{
		// Arrange
		ServiceCollection services = new();
		services.AddFileSystemProvider();
		using ServiceProvider serviceProvider = services.BuildServiceProvider();

		// Act
		IFileSystemProvider provider1 = serviceProvider.GetRequiredService<IFileSystemProvider>();
		IFileSystemProvider provider2 = serviceProvider.GetRequiredService<IFileSystemProvider>();

		// Assert
		Assert.AreSame(provider1, provider2, "FileSystemProvider should be registered as singleton");
	}

	[TestMethod]
	public void DependencyInjection_Integration_Works()
	{
		// Arrange
		ServiceCollection services = new();
		services.AddFileSystemProvider(TestOptions);
		services.AddTransient<TestService>();

		using ServiceProvider serviceProvider = services.BuildServiceProvider();

		IFileSystemProvider provider = serviceProvider.GetRequiredService<IFileSystemProvider>();
		provider.SetFileSystemFactory(() => new MockFileSystem(new Dictionary<string, MockFileData>
		{
			{ "test.txt", new MockFileData("Hello World") }
		}));

		// Act
		TestService testService = serviceProvider.GetRequiredService<TestService>();
		string content = testService.ReadFile("test.txt");

		// Assert
		Assert.AreEqual("Hello World", content);

		// Cleanup
		provider.ResetToDefault();
	}

	[TestMethod]
	public void ParallelTests_AreIsolated()
	{
		// Arrange
		FileSystemProvider provider = new(TestOptions);
		provider.SetFileSystemFactory(() => new MockFileSystem());

		// Act - Run parallel tests
		Parallel.For(0, 10, i =>
		{
			IFileSystem fileSystem = provider.Current;
			fileSystem.File.WriteAllText($"test{i}.txt", $"content{i}");

			// Each parallel execution gets its own MockFileSystem
			MockFileSystem mockFS = (MockFileSystem)provider.Current;
			Assert.IsTrue(mockFS.File.Exists($"test{i}.txt"));
		});
	}

	[TestMethod]
	public void FileSystemProviderOptions_DefaultValues_AreCorrect()
	{
		// Arrange & Act
		FileSystemProviderOptions options = new();

		// Assert
		Assert.IsTrue(options.ThrowOnTestModeInProduction);
	}

	[TestMethod]
	public void FileSystemProvider_WithOptions_UsesConfiguration()
	{
		// Arrange
		FileSystemProviderOptions options = new()
		{
			ThrowOnTestModeInProduction = false
		};

		// Act
		FileSystemProvider provider = new(options);

		// Assert
		Assert.IsNotNull(provider);
		Assert.IsFalse(provider.IsInTestMode);
	}

	[TestMethod]
	public void IsInTestMode_ReturnsFalse_WhenNoFactorySet()
	{
		// Arrange
		FileSystemProvider provider = new(TestOptions);

		// Act & Assert
		Assert.IsFalse(provider.IsInTestMode);
	}

	[TestMethod]
	public void IsInTestMode_ReturnsTrue_WhenFactorySet()
	{
		// Arrange
		FileSystemProvider provider = new(TestOptions);

		// Act
		provider.SetFileSystemFactory(() => new MockFileSystem());

		// Assert
		Assert.IsTrue(provider.IsInTestMode);

		// Cleanup
		provider.ResetToDefault();
	}

	[TestMethod]
	public void SetFileSystemFactory_ThrowsException_WhenFactoryReturnsNull()
	{
		// Arrange
		FileSystemProviderOptions options = new()
		{
			ThrowOnTestModeInProduction = false
		};
		FileSystemProvider provider = new(options);

		// Act
		provider.SetFileSystemFactory(() => null!);

		// Assert - Exception should be thrown when accessing Current
		Assert.ThrowsException<ArgumentNullException>(() => _ = provider.Current);
	}

	[TestMethod]
	public void AddFileSystemProvider_WithOptions_WorksCorrectly()
	{
		// Arrange
		ServiceCollection services = new();
		FileSystemProviderOptions options = new()
		{
			ThrowOnTestModeInProduction = false
		};

		// Act
		services.AddFileSystemProvider(options);
		using ServiceProvider serviceProvider = services.BuildServiceProvider();

		// Assert
		IFileSystemProvider provider = serviceProvider.GetRequiredService<IFileSystemProvider>();
		Assert.IsNotNull(provider);
		Assert.IsInstanceOfType<FileSystemProvider>(provider);
	}

	[TestMethod]
	public void AddFileSystemProvider_WithOptionsAction_WorksCorrectly()
	{
		// Arrange
		ServiceCollection services = new();

		// Act
		services.AddFileSystemProvider(options => options.ThrowOnTestModeInProduction = false);
		using ServiceProvider serviceProvider = services.BuildServiceProvider();

		// Assert
		IFileSystemProvider provider = serviceProvider.GetRequiredService<IFileSystemProvider>();
		Assert.IsNotNull(provider);
		Assert.IsInstanceOfType<FileSystemProvider>(provider);
	}

	[TestMethod]
	public void FileSystemProviderException_HasCorrectProperties()
	{
		// Arrange
		InvalidOperationException innerException = new("Inner");

		// Act
		FileSystemProviderException exception = new(
			FileSystemProviderExceptionType.FactoryReturnsNull,
			"Test message",
			innerException);

		// Assert
		Assert.AreEqual(FileSystemProviderExceptionType.FactoryReturnsNull, exception.ExceptionType);
		Assert.AreEqual("Test message", exception.Message);
		Assert.AreSame(innerException, exception.InnerException);
	}

	[TestCleanup]
	public void TestCleanup()
	{
		// Tests are now isolated by design - no global state to clean up
	}
}

// Test service for dependency injection integration test
public class TestService(IFileSystemProvider fileSystemProvider)
{
	private readonly IFileSystemProvider _fileSystemProvider = fileSystemProvider;

	public string ReadFile(string path)
	{
		return _fileSystemProvider.Current.File.ReadAllText(path);
	}
}
