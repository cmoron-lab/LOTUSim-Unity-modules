// Copyright (c) 2026 Cyril Moron — EPL-2.0
// Mapping vitesse/barre → débit de particules. Fonctions pures, sans
// dépendance MonoBehaviour : c'est la seule partie de l'effet qui se teste
// hors éditeur, et donc la seule qui a un filet.
using UnityEngine;

public static class WakeMath
{
    /// Vitesse normalisée dans [0,1] : plein régime à refSpeed et au-delà.
    public static float SpeedFactor(float speed, float refSpeed)
    {
        if (refSpeed <= 0f) return 0f;   // knob mis à zéro : on éteint, on ne divise pas
        return Mathf.Clamp01(speed / refSpeed);
    }

    /// Sillage de coque : linéaire en vitesse.
    public static float WakeRate(float speed, float refSpeed, float maxRate)
    {
        return maxRate * SpeedFactor(speed, refSpeed);
    }

    /// Écume d'étrave : loi carrée. Elle monte plus tard que le sillage mais
    /// plus fort — c'est ce contraste qui donne la sensation de vitesse.
    public static float BowRate(float speed, float refSpeed, float maxRate)
    {
        float f = SpeedFactor(speed, refSpeed);
        return maxRate * f * f;
    }

    /// Remous de safran : exige du flux ET de la déflexion. Barre au centre
    /// ou bateau à l'arrêt : rien.
    public static float RudderRate(float speed, float refSpeed, float rudderDeg,
                                   float maxRudderDeg, float maxRate)
    {
        if (maxRudderDeg <= 0f) return 0f;
        float helm = Mathf.Clamp01(Mathf.Abs(rudderDeg) / maxRudderDeg);
        return maxRate * SpeedFactor(speed, refSpeed) * helm;
    }
}
