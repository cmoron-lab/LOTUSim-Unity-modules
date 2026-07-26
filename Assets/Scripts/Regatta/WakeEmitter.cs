// Copyright (c) 2026 Cyril Moron — EPL-2.0
// Sillage, écume d'étrave et remous de safran pour la regatta, pilotés par le
// mouvement propre du bateau — aucune souscription ROS supplémentaire.
// Attacher à la RACINE DU PREFAB focus_v2, à côté d'ActuatorAnimator : les
// ancrages sont trouvés par nom, il n'y a rien à câbler.
//
// Les deux réglages qui font ou défont l'effet :
//   - simulationSpace = World, sinon les particules suivent la coque et le
//     "sillage" devient une écharpe collée au bateau ;
//   - émetteurs clampés à seaLevel, sinon le sillage part en l'air dès que le
//     bateau gîte — et il gîte beaucoup, c'est le sujet du scénario.
using UnityEngine;

public class WakeEmitter : MonoBehaviour
{
    public float seaLevel = 0f;         // plan d'eau de référence (monde)
    public float refSpeed = 3f;         // m/s : débit plein à cette vitesse
    public float wakeRate = 60f;        // particules/s max, 0 = émetteur éteint
    public float bowRate = 40f;
    public float rudderRate = 25f;
    public float maxRudderDeg = 35f;    // barre à fond
    public float particleSize = 0.12f;
    public float lifetime = 4f;
    public float smoothing = 0.15f;     // lissage de la vitesse (s)

    const string MaterialName = "RegattaSpray";

    ParticleSystem _wake, _bow, _swirl;
    Transform _hull, _bowNose, _rudder;
    ActuatorAnimator _actuator;
    Vector3 _lastPos;
    float _speed;

    void Start()
    {
        var mat = Resources.Load<Material>(MaterialName);
        if (mat == null)
        {
            Debug.LogError($"WakeEmitter: material '{MaterialName}' not found in " +
                           "Assets/Resources — disabling. Three emitters without a " +
                           "material render as magenta; better to show nothing.");
            enabled = false;
            return;
        }

        _hull = FindPart("Hull");
        _bowNose = FindPart("BowNose");
        _rudder = FindPart("Rudder");
        _actuator = GetComponent<ActuatorAnimator>();
        if (_actuator == null)
            Debug.LogWarning("WakeEmitter: no ActuatorAnimator on this object — " +
                             "rudder swirl disabled, wake and bow unaffected.");

        _lastPos = transform.position;

        // Étalement latéral large et vitesse initiale faible : le sillage
        // s'ouvre en V derrière la coque au lieu d'être un jet.
        _wake = MakeSystem("WakeParticles", mat, spread: 35f, speed: 0.25f);
        _bow = MakeSystem("BowParticles", mat, spread: 25f, speed: 0.6f);
        _swirl = MakeSystem("RudderParticles", mat, spread: 15f, speed: 0.4f);
    }

    void Update()
    {
        // Vitesse horizontale uniquement : quand la houle arrivera (PR #35), le
        // pilonnement ne doit pas se lire comme de la vitesse et déclencher du
        // sillage sur un bateau à l'arrêt.
        Vector3 delta = transform.position - _lastPos;
        _lastPos = transform.position;
        delta.y = 0f;
        float instant = Time.deltaTime > 0f ? delta.magnitude / Time.deltaTime : 0f;
        _speed = Mathf.Lerp(_speed, instant,
                            smoothing > 0f ? Time.deltaTime / smoothing : 1f);

        float helm = _actuator != null ? _actuator.RudderAngle : 0f;

        Place(_wake, _hull);
        Place(_bow, _bowNose);
        Place(_swirl, _rudder);

        // Ancrage introuvable → cet émetteur reste muet. Sans ce garde il
        // cracherait des particules à l'origine du prefab, ce qui est pire
        // qu'un effet manquant : ça ressemble à un bug de rendu.
        SetRate(_wake, _hull != null
            ? WakeMath.WakeRate(_speed, refSpeed, wakeRate) : 0f);
        SetRate(_bow, _bowNose != null
            ? WakeMath.BowRate(_speed, refSpeed, bowRate) : 0f);
        SetRate(_swirl, _rudder != null
            ? WakeMath.RudderRate(_speed, refSpeed, helm, maxRudderDeg, rudderRate) : 0f);
    }

    // Suit la partie en x/z mais reste à la flottaison : c'est le clamp qui
    // empêche le sillage de décoller quand la coque se couche.
    void Place(ParticleSystem ps, Transform anchor)
    {
        if (ps == null || anchor == null) return;
        Vector3 p = anchor.position;
        p.y = seaLevel;
        ps.transform.position = p;
    }

    static void SetRate(ParticleSystem ps, float rate)
    {
        if (ps == null) return;
        var emission = ps.emission;
        emission.rateOverTime = rate;
    }

    ParticleSystem MakeSystem(string name, Material mat, float spread, float speed)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, worldPositionStays: false);
        var ps = go.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = lifetime;
        main.startSize = particleSize;
        main.startSpeed = speed;
        main.maxParticles = 2000;
        main.playOnAwake = true;

        var emission = ps.emission;
        emission.rateOverTime = 0f;   // muet tant que le bateau ne bouge pas

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = spread;
        shape.radius = 0.05f;

        ps.GetComponent<ParticleSystemRenderer>().material = mat;
        return ps;
    }

    Transform FindPart(string name)
    {
        foreach (var t in GetComponentsInChildren<Transform>(true))
            if (t.name == name) return t;
        Debug.LogWarning($"WakeEmitter: '{name}' not found under {transform.name} — " +
                         "that emitter stays silent, the others are unaffected.");
        return null;
    }
}
