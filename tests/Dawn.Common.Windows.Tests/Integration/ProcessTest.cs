using System.Diagnostics;
using Dawn.Common.Windows.Extensions;
using FluentAssertions;

namespace Dawn.Common.Windows.Tests.Integration;

[TestFixture(TestOf = typeof(ProcessEx))]
public class ProcessTest
{
    [Test]
    public void Should_GetParentProcessId()
    {
        // Arrange
        var proc = Process.GetCurrentProcess();
        
        // Act
        var parentId = proc.ParentId;

        // Assert
        parentId.Success
            .Should().Be(true);
        
        parentId.Value
            .Should().NotBe(-1)
            .And.Subject.Should().NotBe(proc.Id);
        
        Assert.Pass($"Parent id: {parentId}");
    }
    
    [Test]
    public void Should_GetParentProcess()
    {
        // Arrange
        var proc = Process.GetCurrentProcess();
        
        // Act
        var parent = proc.GetParentProcess();

        // Assert
        parent.Success
            .Should().Be(true);
        parent
            .Should().NotBeNull();
        
        Assert.Pass($"Parent: {parent.Value!.ProcessName}");
    }

    [Test]
    public void Should_Evict_ProcessCache()
    {
        // Arrange
        var procs = Process.GetProcesses().ToArray();
        var sut = ProcessEx._parentProcessIdCache;
        
        // Act
        var index = 0;
        foreach (var proc in procs)
        {
            index++;
            _ = proc.ParentId;
            if (sut.Count >= ProcessEx.EVICTION_LIMIT)
                break;
        }

        var oldest = sut.First().Key;
        var secondOldest = sut.Skip(1).First().Key;
        
        foreach (var proc in procs.Skip(index))
        {
            _ = proc.ParentId;
            if (sut.First().Key != oldest)
                break;
        }
            
        // Assert
        sut.Count
            .Should().Be(ProcessEx.EVICTION_LIMIT);
        sut.ContainsKey(oldest)
            .Should().BeFalse("cache should be evicted");
        
        sut.ContainsKey(secondOldest)
            .Should().BeTrue("a cache eviction should move the 2nd oldest to the oldest slot");

        sut.First().Key.PID
            .Should().Be(secondOldest.PID, "a cache eviction should move the 2nd oldest to the oldest slot");

    }
}