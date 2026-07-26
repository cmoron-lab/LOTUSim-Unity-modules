// Copyright (c) 2026 Cyril Moron — EPL-2.0
using NUnit.Framework;

public class WakeMathTests
{
    const float Ref = 3f;      // m/s, vitesse de normalisation
    const float Max = 60f;     // particules/s à plein régime
    const float MaxHelm = 35f; // deg, barre à fond
    const float Eps = 1e-4f;

    [Test]
    public void AucunDebitALArret()
    {
        Assert.AreEqual(0f, WakeMath.WakeRate(0f, Ref, Max), Eps);
        Assert.AreEqual(0f, WakeMath.BowRate(0f, Ref, Max), Eps);
        Assert.AreEqual(0f, WakeMath.RudderRate(0f, Ref, MaxHelm, MaxHelm, Max), Eps);
    }

    [Test]
    public void LeDebitCroitAvecLaVitesse()
    {
        Assert.Less(WakeMath.WakeRate(1f, Ref, Max), WakeMath.WakeRate(2f, Ref, Max));
        Assert.Less(WakeMath.BowRate(1f, Ref, Max), WakeMath.BowRate(2f, Ref, Max));
    }

    [Test]
    public void LeDebitEstClampeAuDelaDeRefSpeed()
    {
        // Une survitesse ne doit pas faire exploser le nombre de particules.
        Assert.AreEqual(Max, WakeMath.WakeRate(Ref * 10f, Ref, Max), Eps);
        Assert.AreEqual(Max, WakeMath.BowRate(Ref * 10f, Ref, Max), Eps);
    }

    [Test]
    public void LEtraveEstConvexeParRapportAuSillage()
    {
        // La loi carrée : à mi-vitesse l'étrave est en retrait, à pleine
        // vitesse elle rejoint le sillage. C'est ce qui fait qu'un bateau
        // rapide se lit comme rapide, et pas seulement comme en mouvement.
        Assert.Less(WakeMath.BowRate(Ref / 2f, Ref, Max),
                    WakeMath.WakeRate(Ref / 2f, Ref, Max));
        Assert.AreEqual(WakeMath.WakeRate(Ref, Ref, Max),
                        WakeMath.BowRate(Ref, Ref, Max), Eps);
    }

    [Test]
    public void BarreAuCentreAucunRemous()
    {
        Assert.AreEqual(0f, WakeMath.RudderRate(Ref, Ref, 0f, MaxHelm, Max), Eps);
    }

    [Test]
    public void LeRemousDeSafranExigeDuFlux()
    {
        // Barre à fond mais bateau à l'arrêt : pas d'eau qui passe, pas de remous.
        Assert.AreEqual(0f, WakeMath.RudderRate(0f, Ref, MaxHelm, MaxHelm, Max), Eps);
    }
}
