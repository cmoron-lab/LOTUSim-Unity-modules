// Copyright (c) 2026 Cyril Moron — EPL-2.0
// Pure logic only: the cycle math and the resolution-scale math. The rest of
// RenderBudget is Unity-API glue (PlayerPrefs, QualitySettings, Screen,
// Display, WaterSurface, scene lookups) that needs a running player to
// exercise meaningfully -- not covered here.
using System.Reflection;
using UnityEngine;
using NUnit.Framework;

public class RenderBudgetTests
{
    static object Invoke(string name, params object[] args)
    {
        System.Type renderBudget =
            System.Type.GetType("RenderBudget, Assembly-CSharp");
        Assert.IsNotNull(renderBudget);
        MethodInfo method = renderBudget.GetMethod(
            name, BindingFlags.NonPublic | BindingFlags.Static);
        Assert.IsNotNull(method);
        return method.Invoke(null, args);
    }

    static T[] StaticField<T>(string name)
    {
        System.Type renderBudget =
            System.Type.GetType("RenderBudget, Assembly-CSharp");
        Assert.IsNotNull(renderBudget);
        FieldInfo field = renderBudget.GetField(
            name, BindingFlags.NonPublic | BindingFlags.Static);
        Assert.IsNotNull(field);
        return (T[])field.GetValue(null);
    }

    [TestCase(0, 3, 1)]
    [TestCase(1, 3, 2)]
    [TestCase(2, 3, 0)]
    public void NextCycleIndexWrapsAroundLength(int current, int length, int expected)
    {
        Assert.AreEqual(expected, Invoke("NextCycleIndex", current, length));
    }

    [Test]
    public void FrameCapCycleIs30Then60ThenUncapped()
    {
        int[] caps = StaticField<int>("FrameCaps");
        CollectionAssert.AreEqual(new[] { 30, 60, -1 }, caps);
    }

    [Test]
    public void RenderScaleCycleIs100Then75Then50Percent()
    {
        float[] scales = StaticField<float>("RenderScales");
        CollectionAssert.AreEqual(new[] { 1f, 0.75f, 0.5f }, scales);
    }

    [TestCase(1920, 1f, 1920)]
    [TestCase(1920, 0.75f, 1440)]
    [TestCase(1920, 0.5f, 960)]
    [TestCase(1921, 0.75f, 1441)]
    public void ScaledDimensionRoundsToNearestPixel(int native, float scale, int expected)
    {
        Assert.AreEqual(expected, Invoke("ScaledDimension", native, scale));
    }
}
