using AIQ.Urban.RhinoInterface;
using NUnit.Framework;
using System;
using System.Linq;

namespace AIQ.Urban.RhinoInterface.Tests;

/// <summary>
/// Tests for EventQueue — pure logic with no Rhino dependency.
/// </summary>
public class EventQueueTests
{
    [SetUp]
    public void SetUp()
    {
        _queue = new EventQueue();
    }

    private EventQueue _queue = null!;

    [Test]
    public void Drain_EmptyQueue_ReturnsEmptyBatch()
    {
        var batch = _queue.Drain();

        Assert.That(batch.Adds, Is.Empty);
        Assert.That(batch.Deletes, Is.Empty);
        Assert.That(batch.Replacements, Is.Empty);
        Assert.That(_queue.IsEmpty);
    }

    [Test]
    public void Drain_SecondCall_ReturnsEmpty()
    {
        _queue.EnqueueAdd(Guid.NewGuid());
        _queue.Drain();

        var second = _queue.Drain();

        Assert.That(second.Adds, Is.Empty);
        Assert.That(second.Deletes, Is.Empty);
        Assert.That(second.Replacements, Is.Empty);
    }

    [Test]
    public void EnqueueAdd_Drain_ReturnsAdded()
    {
        var id = Guid.NewGuid();
        _queue.EnqueueAdd(id);

        var batch = _queue.Drain();

        Assert.That(batch.Adds, Has.Count.EqualTo(1));
        Assert.That(batch.Adds[0], Is.EqualTo(id));
    }

    [Test]
    public void EnqueueDelete_Drain_ReturnsDeleted()
    {
        var id = Guid.NewGuid();
        _queue.EnqueueDelete(id);

        var batch = _queue.Drain();

        Assert.That(batch.Deletes, Has.Count.EqualTo(1));
        Assert.That(batch.Deletes[0], Is.EqualTo(id));
    }

    [Test]
    public void EnqueueReplace_Drain_ReturnsReplacement()
    {
        var oldId = Guid.NewGuid();
        var newId = Guid.NewGuid();
        _queue.EnqueueReplace(oldId, newId);

        var batch = _queue.Drain();

        Assert.That(batch.Replacements, Has.Count.EqualTo(1));
        Assert.That(batch.Replacements[0].OldId, Is.EqualTo(oldId));
        Assert.That(batch.Replacements[0].NewId, Is.EqualTo(newId));
    }

    [Test]
    public void AddThenDeleteSameId_CancelsBoth()
    {
        var id = Guid.NewGuid();
        _queue.EnqueueAdd(id);
        _queue.EnqueueDelete(id);

        var batch = _queue.Drain();

        Assert.That(batch.Adds, Is.Empty);
        Assert.That(batch.Deletes, Is.Empty);
    }

    [Test]
    public void DeleteThenAddSameId_CancelsBoth()
    {
        var id = Guid.NewGuid();
        _queue.EnqueueDelete(id);
        _queue.EnqueueAdd(id);

        var batch = _queue.Drain();

        Assert.That(batch.Adds, Is.Empty);
        Assert.That(batch.Deletes, Is.Empty);
    }

    [Test]
    public void AddThenReplaceSameId_ReplaceWins()
    {
        var id = Guid.NewGuid();
        var newId = Guid.NewGuid();
        _queue.EnqueueAdd(id);
        _queue.EnqueueReplace(id, newId);

        var batch = _queue.Drain();

        Assert.That(batch.Adds, Is.Empty);
        Assert.That(batch.Replacements, Has.Count.EqualTo(1));
        Assert.That(batch.Replacements[0].OldId, Is.EqualTo(id));
        Assert.That(batch.Replacements[0].NewId, Is.EqualTo(newId));
    }

    [Test]
    public void ReplaceThenDeleteSameOldId_DeleteWins()
    {
        var oldId = Guid.NewGuid();
        var newId = Guid.NewGuid();
        _queue.EnqueueReplace(oldId, newId);
        _queue.EnqueueDelete(oldId);

        var batch = _queue.Drain();

        Assert.That(batch.Replacements, Is.Empty);
        Assert.That(batch.Deletes, Has.Count.EqualTo(1));
        Assert.That(batch.Deletes[0], Is.EqualTo(oldId));
    }

    [Test]
    public void ReplaceThenDeleteNewId_CancelsReplace_KeepsDelete()
    {
        var oldId = Guid.NewGuid();
        var newId = Guid.NewGuid();
        _queue.EnqueueReplace(oldId, newId);
        _queue.EnqueueDelete(newId);

        var batch = _queue.Drain();

        // Replacement cancelled since new object was deleted
        Assert.That(batch.Replacements, Is.Empty);
        // NewId stays as a standalone delete
        Assert.That(batch.Deletes, Has.Count.EqualTo(1));
        Assert.That(batch.Deletes[0], Is.EqualTo(newId));
    }

    [Test]
    public void MultipleReplacesSameOldId_OnlyLastNewIdKept()
    {
        var oldId = Guid.NewGuid();
        var newId1 = Guid.NewGuid();
        var newId2 = Guid.NewGuid();

        _queue.EnqueueReplace(oldId, newId1);
        _queue.EnqueueReplace(oldId, newId2);

        var batch = _queue.Drain();

        Assert.That(batch.Replacements, Has.Count.EqualTo(1));
        Assert.That(batch.Replacements[0].OldId, Is.EqualTo(oldId));
        Assert.That(batch.Replacements[0].NewId, Is.EqualTo(newId2));
    }

    [Test]
    public void MultipleIndependentEvents_AllPresent()
    {
        var addId = Guid.NewGuid();
        var delId = Guid.NewGuid();
        var oldId = Guid.NewGuid();
        var newId = Guid.NewGuid();

        _queue.EnqueueAdd(addId);
        _queue.EnqueueDelete(delId);
        _queue.EnqueueReplace(oldId, newId);

        var batch = _queue.Drain();

        Assert.That(batch.Adds, Has.Count.EqualTo(1));
        Assert.That(batch.Adds[0], Is.EqualTo(addId));
        Assert.That(batch.Deletes, Has.Count.EqualTo(1));
        Assert.That(batch.Deletes[0], Is.EqualTo(delId));
        Assert.That(batch.Replacements, Has.Count.EqualTo(1));
        Assert.That(batch.Replacements[0].OldId, Is.EqualTo(oldId));
    }

    [Test]
    public void Clear_EmptiesAllQueues()
    {
        _queue.EnqueueAdd(Guid.NewGuid());
        _queue.EnqueueDelete(Guid.NewGuid());
        _queue.EnqueueReplace(Guid.NewGuid(), Guid.NewGuid());

        _queue.Clear();

        Assert.That(_queue.IsEmpty);
    }

    [Test]
    public void AddThenReplaceThenDeleteSameOldId_DeleteWins()
    {
        var oldId = Guid.NewGuid();
        var newId = Guid.NewGuid();

        _queue.EnqueueAdd(oldId);
        _queue.EnqueueReplace(oldId, newId);
        _queue.EnqueueDelete(oldId);

        var batch = _queue.Drain();

        // Step 1: replacement resolves against deletes → oldId in deletes → remove replacement
        // Step 2: add is removed (line in step 1: _adds.Remove(oldId))
        // OldId stays in deletes
        Assert.That(batch.Adds, Is.Empty);
        Assert.That(batch.Replacements, Is.Empty);
        Assert.That(batch.Deletes, Has.Count.EqualTo(1));
        Assert.That(batch.Deletes[0], Is.EqualTo(oldId));
    }

    [Test]
    public void IsEmpty_TrueInitially()
    {
        Assert.That(_queue.IsEmpty);
    }

    [Test]
    public void IsEmpty_FalseAfterEnqueue()
    {
        _queue.EnqueueAdd(Guid.NewGuid());
        Assert.That(_queue.IsEmpty, Is.False);
    }

    [Test]
    public void IsEmpty_TrueAfterDrain()
    {
        _queue.EnqueueAdd(Guid.NewGuid());
        _queue.Drain();
        Assert.That(_queue.IsEmpty);
    }
}
