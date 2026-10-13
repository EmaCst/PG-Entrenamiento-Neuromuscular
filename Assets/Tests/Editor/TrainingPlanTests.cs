using System;
using NUnit.Framework;

public class TrainingPlanTests
{
    [Test] public void DisabledExerciseIsSkippedInEverySet()
    {
        var stages = TrainingPlan.Circuit(2, 60f, 0f, 30f);
        Assert.AreEqual(4, stages.Count);
        Assert.AreEqual("TelefonoManos", stages[0].Scene);
        Assert.AreEqual("TelefonoCorrer", stages[1].Scene);
        Assert.AreEqual(1, stages[1].Set);
        Assert.AreEqual(2, stages[2].Set);
        Assert.AreEqual("TelefonoCorrer", stages[3].Scene);
    }
    [Test] public void OneEnabledExerciseRunsOncePerSet()
    {
        var stages = TrainingPlan.Circuit(4, 0f, 60f, 0f);
        Assert.AreEqual(4, stages.Count);
        Assert.AreEqual(4, stages[3].Set);
        foreach (var stage in stages) Assert.AreEqual("TelefonoPies", stage.Scene);
    }
    [Test] public void EmptyCircuitCannotStart()
    {
        Assert.Throws<ArgumentException>(() => TrainingPlan.Circuit(4, 0f, 0f, 0f));
    }
    [TestCase(0)] [TestCase(101)] public void InvalidSetCountCannotStart(int sets)
    {
        Assert.Throws<ArgumentException>(() => TrainingPlan.Circuit(sets, 60f, 60f, 60f));
    }
    [Test] public void InvalidTimesCannotStart()
    {
        foreach (float seconds in new[] { -1f, 1f, 4f, 3601f, float.NaN, float.PositiveInfinity })
            Assert.Throws<ArgumentException>(() => TrainingPlan.Circuit(1, seconds, 60f, 60f));
        Assert.IsTrue(TrainingPlan.ValidRest(0f));
        Assert.IsTrue(TrainingPlan.ValidRest(2f));
        Assert.IsFalse(TrainingPlan.ValidRest(float.NaN));
        Assert.IsFalse(TrainingPlan.ValidRest(-1f));
    }
}
