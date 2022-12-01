using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MapPresence
{
    [Header("Camera")]
    [SerializeField] private float cameraLerp = 5f;

    [Header("Misc")]
    [SerializeField] private Color[] lvlColours;
    [SerializeField] private float scaleIncreasePerLvl = 1.3f;

    [Header("References")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private Transform playerTransform;

    private MaterialPropertyBlock matProps;
    private MeshRenderer matRend;


    private void Awake()
    {
        objectType = ObjectTypes.Player;
        moveable = playerTransform;

        matRend = playerTransform.GetComponent<MeshRenderer>();
        matProps = new MaterialPropertyBlock();

        GameManager.onGameLoad += Setup;
    }

    private void Setup()
    {
        GameManager.scoreCallback += (x, lvl) =>
        {
            matProps.SetColor("_Color", lvlColours[Mathf.Min(lvl, lvlColours.Length - 1)]);
            matRend.SetPropertyBlock(matProps);

            playerTransform.localScale = Vector3.one * Mathf.Pow(scaleIncreasePerLvl, lvl);
        };
        MoveTo(GameManager.instance.GetPlayerSpawn(this));
    }



    private void Update()
    {
        UpdatePlayer();
        UpdateCamera();

        UpdateRelocation();
    }

    private void UpdatePlayer()
    {
        Vector2Int input = new Vector2Int();
        input.x = Input.GetKeyDown(KeyCode.D) ? 1 : Input.GetKeyDown(KeyCode.A) ? -1 : 0;
        input.y = Input.GetKeyDown(KeyCode.W) ? 1 : Input.GetKeyDown(KeyCode.S) ? -1 : 0;

        if (Mathf.Abs(input.magnitude) > 0 && GameManager.instance.TryMovePlayer(mapPos, input))
        {
            GameManager.instance.SwapPlayer(this, mapPos, mapPos + input);
            MoveTo(mapPos + input);
        }
    }


    private void UpdateCamera()
    {
        Quaternion desiredRot = Quaternion.LookRotation(playerTransform.position - cameraTransform.position, Vector3.up);
        cameraTransform.rotation = Quaternion.Lerp(cameraTransform.rotation, desiredRot, Time.deltaTime * cameraLerp);
    }

}
