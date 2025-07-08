using Leap;
using Leap.Unity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ExerciseType {
    ROTATION,
    FIST,
    WRIST_CURL,
    FINGER_CURL,
    WAVE_RELEASE,
    UNDEFINED
}

public class Exercise
{
    public ExerciseType type;
    public bool hasStarted;
    public bool hasFinished;
    public float timeStarted;
    public float timeFinished;

    public Exercise()
    {
        ResetExercise();
    }

    public void StartExercise(ExerciseType type)
    {
        this.type = type;
        this.hasStarted = true;
        this.timeStarted = Time.time;
    }

    public void FinishExercise()
    {
        this.hasFinished = true;
        this.timeFinished = Time.time;
    }

    public void ResetExercise()
    {
        this.type = ExerciseType.UNDEFINED;
        this.hasStarted = false;
        this.hasFinished = false;
        this.timeStarted = -1;
        this.timeFinished = -1;
    }
}

public class ExerciseDetector : MonoBehaviour
{
    public GameObject playerRig;
    public GameObject leftHandObject;
    public GameObject aim;
    public GameObject shield;
    public GameObject chainLightning;
    public GameObject boulder;
    public GameObject kamehameha;
    public GameObject wave;
    public Camera camera;
    public static List<ExerciseType> availableMagics;

    LeapProvider provider;
    GameObject sphere;
    GameObject fingerCurlIndicator;
    int fingerCurlNextFinger;
    bool fingerCurlReadyToFire = false;
    bool fingerCurlFired = false;
    Exercise currentExercise;
    GameObject blast;
    GameObject waveChargeEffect;
    bool waveFirstChargeComplete = false;
    bool waveSecondChargeComplete = false;



    Color phase1Color = new Color(0f, 1f, 1f, 0.4f);   // Ciano translúcido
    Color phase2Color = new Color(0f, 0.4f, 1f, 0.4f); // Azul translúcido
    Color phase3Color = new Color(0f, 1f, 0f, 0.4f);   // Verde translúcido



    float GRAB_TRESHHOLD = 0.063f;
    float ROTATION_UPPER_TRESHHOLD = 3f;
    float ROTATION_LOWER_TRESHHOLD = 0.1f;
    float WRIST_CURL_LOWER_TRESHHOLD = -25f;
    float WRIST_CURL_UPPER_TRESHHOLD = 40f;

    void Start()
    {
        provider = FindObjectOfType<LeapProvider>();
        aim = GameObject.Instantiate(aim);
        aim.SetActive(false);
        shield = GameObject.Instantiate(shield, camera.transform);
        shield.SetActive(false);
        currentExercise = new Exercise();
        availableMagics = new List<ExerciseType>();
    }

    void Update()
    {
        Hand rightHand = null;
        Hand leftHand = null;
        Frame frame = provider.CurrentFrame;

        if (frame.Hands.Capacity > 0)
        {
            foreach (Hand h in frame.Hands)
            {
                if (h.IsLeft)
                    leftHand = h;
                if (h.IsRight)
                    rightHand = h;
            }
        }

        if (leftHand != null && rightHand != null)
        {
            if (currentExercise.hasStarted && !currentExercise.hasFinished)
            {
                switch (currentExercise.type)
                {
                    case ExerciseType.FIST:
                        ProcessFistExercise(leftHand);
                        break;
                    case ExerciseType.ROTATION:
                        ProcessRotationExercise(leftHand);
                        break;
                    case ExerciseType.WRIST_CURL:
                        ProcessWristCurlExercise(leftHand);
                        break;
                    case ExerciseType.FINGER_CURL:
                        ProcessFingerCurlExercise(leftHand);
                        break;
                    case ExerciseType.WAVE_RELEASE:
                        ProcessWaveReleaseExercise(leftHand, rightHand);
                        break;
                }
            }
            else
            {
                currentExercise.ResetExercise();
                ProcessExercises(leftHand, rightHand);
            }
        }

        if (rightHand != null && currentExercise != null && currentExercise.hasStarted && IsHandClosed(rightHand) && blast == null)
        {
            CancelMagic();
        }
    }

    void CancelMagic()
    {
        currentExercise.FinishExercise();
        aim?.SetActive(false);
        shield?.SetActive(false);
        waveChargeEffect?.SetActive(false);
        if (fingerCurlIndicator != null)
            DestroyImmediate(fingerCurlIndicator);
        var magics = GameObject.FindGameObjectsWithTag("Magic");
        foreach (var magic in magics)
        {
            DestroyImmediate(magic);
        }
    }

    void ProcessWaveReleaseExercise(Hand left, Hand right)
    {
        bool leftOpen = IsHandOpened(left);
        bool rightOpen = IsHandOpened(right);

        Vector3 leftDir = left.PalmNormal.ToVector3();
        Vector3 rightDir = right.PalmNormal.ToVector3();

        float leftDot = Vector3.Dot(leftDir, camera.transform.forward);
        float rightDot = Vector3.Dot(rightDir, camera.transform.forward);
        float leftBackDot = Vector3.Dot(leftDir, -camera.transform.forward);
        float rightBackDot = Vector3.Dot(rightDir, -camera.transform.forward);

        float forwardAvg = (leftDot + rightDot) / 2f;
        float backAvg = (leftBackDot + rightBackDot) / 2f;

        float distance = Vector3.Distance(left.PalmPosition.ToVector3(), right.PalmPosition.ToVector3());
        Vector3 center = (left.PalmPosition.ToVector3() + right.PalmPosition.ToVector3()) / 2f;

        float maxScale = 0.1f;

        // FASE 1 – costas das mãos
        if (!waveFirstChargeComplete && leftOpen && rightOpen && backAvg > 0.7f)
        {
            if (currentExercise.type != ExerciseType.WAVE_RELEASE)
            {
                Debug.Log("[WAVE] Iniciando Fase 1 do carregamento (costas das mãos).");
                currentExercise.StartExercise(ExerciseType.WAVE_RELEASE);

                waveChargeEffect = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                ApplyTransparentColor(waveChargeEffect, phase1Color);

                waveChargeEffect.transform.localScale = Vector3.one * 0.017f;
            }
            else
            {
                if (waveChargeEffect?.transform.localScale.x < maxScale)
                {
                    float current = waveChargeEffect.transform.localScale.x;
                    waveChargeEffect.transform.localScale *= (current + 0.001f) / current;
                }
                else
                {
                    Debug.Log("[WAVE] Fase 1 completa.");
                    waveFirstChargeComplete = true;
                    ApplyTransparentColor(waveChargeEffect, phase2Color);
                    waveChargeEffect.transform.localScale = Vector3.one * 0.017f; // reinicia para Fase 2
                }
            }
        }

        // FASE 2 – palmas das mãos
        else if (waveFirstChargeComplete && !waveSecondChargeComplete && leftOpen && rightOpen && forwardAvg > 0.6f)
        {
            if (waveChargeEffect?.transform.localScale.x < maxScale)
            {
                float current = waveChargeEffect.transform.localScale.x;
                waveChargeEffect.transform.localScale *= (current + 0.001f) / current;
            }
            else
            {
                Debug.Log("[WAVE] Fase 2 completa. Pronto para disparar ao juntar as mãos!");
                waveSecondChargeComplete = true;
                ApplyTransparentColor(waveChargeEffect, phase3Color);
            }
        }

        // GATILHO FINAL – juntar as mãos
        else if (waveFirstChargeComplete && waveSecondChargeComplete && distance < 0.1f)
        {
            Debug.Log("[WAVE] Liberação de Onda ativada!");
            currentExercise.FinishExercise();

            if (wave != null)
            {
                Vector3 spawnPosition = playerRig.transform.position + playerRig.transform.forward * 0.5f;
                GameObject w = Instantiate(wave, spawnPosition, Quaternion.identity);
                Destroy(w, 5f);
            }

            if (waveChargeEffect != null)
            {
                Destroy(waveChargeEffect);
                waveChargeEffect = null;
            }

            // Reset flags
            waveFirstChargeComplete = false;
            waveSecondChargeComplete = false;
        }

        // Sempre atualizar posição da esfera entre as mãos
        if (waveChargeEffect != null)
        {
            waveChargeEffect.transform.position = center;
        }
    }



    bool IsHandClosed(Hand hand) {
        int fingersGrasping = 0;

        if (hand != null) {
            Vector3 palm = new Vector3(hand.PalmPosition.x, hand.PalmPosition.y, hand.PalmPosition.z);
            foreach (Finger f in hand.Fingers) {
                Vector3 tip = new Vector3(f.TipPosition.x, f.TipPosition.y, f.TipPosition.z);
                var dist = Vector3.Distance(tip, palm);
                if (dist < GRAB_TRESHHOLD) {
                    fingersGrasping++;
                }
            }
        }

        return fingersGrasping == 5;
    }

    bool IsHandOpened(Hand hand) {
        int fingersExtended = 0;

        if (hand != null) {
            foreach (Finger f in hand.Fingers) {
                if (f.IsExtended) {
                    fingersExtended++;
                }
            }
        }

        return fingersExtended == 5;
    }

    bool IsHandPointingForward(Hand hand) {
        float dot = Vector3.Dot(hand.PalmNormal.ToVector3(), camera.transform.forward);
        return dot > 0;
    }

    void ProcessFistExercise(Hand hand) {
        if (IsHandClosed(hand) && IsHandPointingForward(hand)) {
            if (currentExercise.type != ExerciseType.FIST) {
                currentExercise.StartExercise(ExerciseType.FIST);
                sphere = GameObject.Instantiate(chainLightning);
            }
            else {
                if (sphere?.transform.localScale.x < 0.08) {
                    sphere.transform.localScale *= (sphere.transform.localScale.x + 0.001f) / sphere.transform.localScale.x;
                }
            }
        }
        else if (currentExercise.hasStarted && IsHandOpened(hand) && IsHandPointingForward(hand) && sphere?.transform.localScale.x >= 0.08){
            currentExercise.FinishExercise();
            if (sphere != null) {
                sphere.GetComponent<ChainLightning>().LightItUp(hand);
                GameObject.DestroyImmediate(sphere);
                sphere = null;
            }
        }

        if(sphere != null) {
            sphere.transform.position = hand.PalmPosition.ToVector3() + hand.PalmNormal.ToVector3().normalized * 0.15f;
        }
    }

    void CameraAim() {
        RaycastHit hit;
        int layerMask = 1 << 8; // Only collides with ground
        if (Physics.Raycast(camera.transform.position, camera.transform.forward, out hit, Mathf.Infinity, layerMask)) {
            aim.transform.position = hit.point;
            if (!aim.activeSelf) {
                aim.SetActive(true);
            }
        }
        else {
            aim.SetActive(false);
        }
    }

    GameObject SummonBoulder(GameObject aim) {
        GameObject rock = GameObject.Instantiate(boulder);
        rock.transform.position = aim.transform.position;
        rock.transform.SetLocalY(25f);
        float radius = aim.transform.localScale.x;
        rock.transform.localScale *= radius;
        return rock;
    }

    void ProcessRotationExercise(Hand hand) {
        if (!currentExercise.hasStarted && Mathf.Abs(hand.PalmNormal.Roll) > ROTATION_UPPER_TRESHHOLD && IsHandOpened(hand) && !IsHandPointingForward(hand)) {
            currentExercise.StartExercise(ExerciseType.ROTATION);
            shield.SetActive(true);
        }
        if (currentExercise.hasStarted && Mathf.Abs(hand.PalmNormal.Roll) < ROTATION_LOWER_TRESHHOLD && IsHandOpened(hand) && IsHandPointingForward(hand)) {
            currentExercise.FinishExercise();
            var audios = shield.GetComponents<AudioSource>();
            AudioSource.PlayClipAtPoint(audios[audios.Length - 1].clip, shield.transform.position);
            shield.SetActive(false);
        }
    }

    void ProcessWristCurlExercise(Hand hand) {
        float angle = Vector3.SignedAngle(hand.Arm.Direction.ToVector3(), hand.Direction.ToVector3(), hand.RadialAxis());
        if (currentExercise.hasStarted) {
            if (aim.transform.localScale.x < 3) {
                aim.transform.localScale += new Vector3(0.01f, 0, 0.01f);
            }
            CameraAim();
        }
        else if (!currentExercise.hasStarted && angle < WRIST_CURL_LOWER_TRESHHOLD && !IsHandPointingForward(hand) && hand.GrabStrength > 0.5) {
            currentExercise.StartExercise(ExerciseType.WRIST_CURL);
        }

        if (currentExercise.hasStarted && angle > WRIST_CURL_UPPER_TRESHHOLD && !IsHandPointingForward(hand) && hand.GrabStrength > 0.5 && aim?.transform.localScale.x >= 3) {
            currentExercise.FinishExercise();
            aim.SetActive(false);
            SummonBoulder(aim);
            aim.transform.localScale = new Vector3(0.1f, 0.05f, 0.1f);
        }
    }

    bool IsFingerPinching(Finger finger) {
        Hand hand = provider.CurrentFrame.Hand(finger.HandId);

        for (int i = 1; i < hand.Fingers.Count; i++) {
            Finger f = hand.Fingers[i];
            if (f.Id != finger.Id && !f.IsExtended)
                return false;
        }
        
        return (finger.TipPosition - hand.Fingers[0].TipPosition).MagnitudeSquared < 0.0005;
    }

    void ProcessFingerCurlExercise(Hand hand) {
        
        if (!currentExercise.hasStarted && !IsHandPointingForward(hand) && IsFingerPinching(hand.Fingers[1])) {
            currentExercise.StartExercise(ExerciseType.FINGER_CURL);
            fingerCurlIndicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            fingerCurlIndicator.GetComponent<Renderer>().material.color = Color.red;
            fingerCurlIndicator.transform.localScale = new Vector3(0.02f, 0.02f, 0.02f);
            fingerCurlNextFinger = 2;
            fingerCurlReadyToFire = false;
            fingerCurlFired = false;
        }
        else if (currentExercise.hasStarted && !fingerCurlReadyToFire && !IsHandPointingForward(hand) && IsFingerPinching(hand.Fingers[fingerCurlNextFinger])) {
            fingerCurlIndicator.transform.localScale += new Vector3(0.02f, 0.02f, 0.02f);
            fingerCurlNextFinger++;
            if(fingerCurlNextFinger == 5) {
                fingerCurlReadyToFire = true;
            }
        }

        if (fingerCurlIndicator != null) {
            fingerCurlIndicator.transform.position = hand.PalmPosition.ToVector3() + hand.PalmNormal.ToVector3().normalized * 0.15f;
        }

        if (fingerCurlReadyToFire && !fingerCurlFired && IsHandPointingForward(hand) && IsHandOpened(hand)) {
            Destroy(fingerCurlIndicator);
            blast = Instantiate(kamehameha);
            Destroy(blast, 5);
            fingerCurlFired = true;
        }

        if (blast != null) {
            blast.transform.position = hand.PalmPosition.ToVector3() + hand.PalmNormal.ToVector3().normalized * 0.125f;
            blast.transform.up = hand.PalmNormal.ToVector3();
        }
        else if (fingerCurlFired) {
            currentExercise.FinishExercise();
        }
    }

    void ProcessExercises(Hand left, Hand right)
    {
        if (availableMagics.Contains(ExerciseType.FIST)) ProcessFistExercise(left);
        if (!currentExercise.hasStarted && availableMagics.Contains(ExerciseType.ROTATION)) ProcessRotationExercise(left);
        if (!currentExercise.hasStarted && availableMagics.Contains(ExerciseType.WRIST_CURL)) ProcessWristCurlExercise(left);
        if (!currentExercise.hasStarted && availableMagics.Contains(ExerciseType.FINGER_CURL)) ProcessFingerCurlExercise(left);
        if (!currentExercise.hasStarted && availableMagics.Contains(ExerciseType.WAVE_RELEASE)) ProcessWaveReleaseExercise(left, right);

        Debug.Log(currentExercise.type.ToString());
    }

    void ApplyTransparentColor(GameObject obj, Color color)
    {
        if (obj == null) return;

        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer == null) return;

        Material mat = renderer.material;
        mat.shader = Shader.Find("Standard");
        mat.SetFloat("_Mode", 3); // Transparent
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = 3000;

        mat.color = color;
    }

}