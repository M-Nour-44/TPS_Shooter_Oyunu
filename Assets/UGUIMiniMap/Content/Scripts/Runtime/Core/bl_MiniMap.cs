using Lovatto.MiniMap;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public sealed class bl_MiniMap : MonoBehaviour
{
    #region Public members
    public GameObject m_Target;
    public Transform rotationTargetOverride;
    public int MiniMapLayer = 10;
    public LayerMask excludeLayers;
    public Camera miniMapCamera = null;
    public MiniMapRenderType renderType = MiniMapRenderType.Picture;
    public MiniMapRenderMode canvasRenderMode = MiniMapRenderMode.Mode2D;
    public MiniMapMapType mapMode = MiniMapMapType.Local;
    public MiniMapCameraUpdateMode cameraUpdateMode = MiniMapCameraUpdateMode.EveryFrame;
    public bool Ortographic2D = false;
    public bl_MapRender mapRender = null;
    public bool isMobile = false;
    public int UpdateRate = 5;
    public float playerIconSize = 8;
    [Range(0.05f, 2)] public float IconMultiplier = 1;
    [Range(1, 10)] public int scrollSensitivity = 3;
    public float DefaultHeight = 30;
    public bool saveZoomInRuntime = false;
    public float MaxZoom = 80;
    public float MinZoom = 5;
    public float LerpHeight = 8;
    public bool iconsSizeRelativeToZoom = true;
    public Sprite PlayerIconSprite;
    public MiniMapMapShape mapShape = MiniMapMapShape.Rectangle;
    public float CompassSize = 175f;
    public bool iconsAlwaysFacingUp = true;
    public bool DynamicRotation = true;
    public bool SmoothRotation = true;
    public float LerpRotation = 8;
    public float mapRotationOffset = 0;
    public bool AllowMapMarks = true;
    public GameObject MapPointerPrefab;
    public bool AllowMultipleMarks = false;
    public bool showPathNav = true;
    public bool ShowAreaGrid = true;
    [Range(1, 20)] public float AreasSize = 4;
    public float gridOpacity = 0.7f;
    public float overallOpacity = 1;
    public float backgroundOpacity = 1f;
    public float navPathWidth = 2.5f;

    public MiniMapFullScreenMode fullScreenMode = MiniMapFullScreenMode.ScreenArea;
    public bool FadeOnFullScreen = false;
    public float fullScreenMargin = 10;
    public float sizeTransitionDuration = 0.5f;
    public AnimationCurve sizeTransitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public bool showCursorOnFullscreen = true;

    public bool lerpTrackingPosition = false;
    public Vector3 FullMapPosition = Vector2.zero;
    public Vector3 FullMapRotation = Vector3.zero;
    public Vector2 FullMapSize = Vector2.zero;
    public bool CanDragMiniMap = true;
    public bool DragOnlyOnFullScreen = true;
    public bool ResetOffSetOnChange = true;
    public Vector2 DragMovementSpeed = new Vector2(0.5f, 0.35f);
    public Vector2 MaxOffSetPosition = new Vector2(1000, 1000);
    public Texture2D DragCursorIcon;
    public Vector2 HotSpot = Vector2.zero;
    public float planeSaturation = 1.4f;
    public bl_MiniMapBounds mapBounds;
    public Canvas m_Canvas = null;
    public GameObject ItemPrefabSimple = null;
    public Transform minimapRig;
    public MiniMapRTSize _rtSize = MiniMapRTSize._1024;
    public Color playerColor = Color.white;
    public Color emptySpaceColor = new Color(0, 0, 0, 0.25f);
    public Color navPathColor = Color.green;
    #endregion

    #region Public properties
    public bool IsFullScreen { get; set; }
    public bool hasError { get; set; }

    public float Zoom { get; set; }

    public bool HighPrecisionMode
    {
        get;
        set;
    } = false;

    public static bl_MiniMap ActiveMiniMap { get; private set; }

    private bl_MiniMapUI _minimapUI = null;

    public bl_MiniMapUI MiniMapUI
    {
        get
        {
            if (_minimapUI == null)
            {
                _minimapUI = transform.parent != null
                    ? transform.parent.GetComponentInChildren<bl_MiniMapUI>(true)
                    : GetComponentInChildren<bl_MiniMapUI>(true);
            }

            return _minimapUI;
        }
    }
    #endregion

    #region Private members
    private GameObject mapPointer;
    [HideInInspector] public Vector3 MiniMapPosition = Vector2.zero;
    [HideInInspector] public Vector3 MiniMapRotation = Vector3.zero;
    [HideInInspector] public Vector2 MiniMapSize = Vector2.zero;
    private Vector3 DragOffset = Vector3.zero;
    private bool DefaultRotationMode = false;
    private Vector3 DeafultMapRot = Vector3.zero;
    private MiniMapMapShape defaultShape;
    public const string MMHeightKey = "MinimapCameraHeight";
    private bool isAlphaComplete = false;
    private bool isPlanedCreated = false;
    private readonly List<bl_MiniMapEntityBase> miniMapItems = new List<bl_MiniMapEntityBase>();
    private Vector3 playerPosition;
    private Vector3 targetPosition;
    private Vector3 playerRotation;
    private bool isUpdateFrame = false;
    private bl_MiniMapPlaneBase miniMapPlane;
    [HideInInspector] public bool _isPreviewFullscreen = false;
    private bool m_initialized = false;
    private bl_MiniMapInputBase inputHandler;
    private bool wasCursorVisible = false;
    private CursorLockMode wasCursorMode = CursorLockMode.None;
    private Vector3 m_mapRotationOffsetVector = Vector3.zero;
    private float minimapZoom = 0;
    private bl_MiniMapPathNav pathNav;
    private bl_MiniMapTarget targetScript;
    #endregion

    void Awake()
    {
        if (!m_initialized)
        {
            inputHandler = bl_MiniMapData.Instance.inputHandler;

            if (inputHandler != null)
            {
                inputHandler.Init();
            }

            MiniMapUI?.Setup(this);
            MiniMapUI.MiniMapSize?.Init(this);
            GetMiniMapSize();

            DefaultRotationMode = DynamicRotation;
            DeafultMapRot = minimapRig.eulerAngles;
            defaultShape = mapShape;
            m_mapRotationOffsetVector.Set(0, mapRotationOffset, 0);

            if (m_Target != null)
            {
                m_Target.TryGetComponent(out targetScript);
            }

            if (hasError)
            {
                return;
            }

            mapBounds?.Init();
            SetupMiniMapCamera();
            CreateMapPlane(renderType == MiniMapRenderType.RealTime);

            if (mapMode == MiniMapMapType.Local)
            {
                Zoom = saveZoomInRuntime ? PlayerPrefs.GetFloat(MMHeightKey, DefaultHeight) : DefaultHeight;
            }
            else
            {
                ConfigureWorldTarget();
                Zoom = DefaultHeight;
            }

            minimapZoom = Zoom;
        }

        MiniMapUI.DoStartFade(0, () => { isAlphaComplete = true; });
        m_initialized = true;
    }

    private void Start()
    {
        if (ActiveMiniMap == null)
        {
            ActiveMiniMap = this;
        }
    }

    void OnEnable()
    {
        if (!isAlphaComplete)
        {
            MiniMapUI.DoStartFade(0, () => { isAlphaComplete = true; });
        }
    }

    void CreateMapPlane(bool realTime)
    {
        if (isPlanedCreated)
        {
            return;
        }

        if (mapRender == null && !realTime)
        {
            Debug.LogError("Map Render has not been assigned.");
            return;
        }

        if (!realTime || ShowAreaGrid)
        {
            GameObject plane = Instantiate(bl_MiniMapData.GetMapPlanePrefab().gameObject) as GameObject;
            miniMapPlane = plane.GetComponent<bl_MiniMapPlaneBase>();
            miniMapPlane.Setup(this);
        }

        isPlanedCreated = true;
    }

    private void SetupMiniMapCamera()
    {
        string layer = LayerMask.LayerToName(MiniMapLayer);

        if (string.IsNullOrEmpty(layer))
        {
            int tryID = LayerMask.NameToLayer("MiniMap");

            if (tryID == -1)
            {
                Debug.LogError($"MiniMap Layer '{tryID}' is null, please assign it in the inspector.", gameObject);
                MiniMapUI.SetActive(false);
                hasError = true;
                enabled = false;
                return;
            }
            else
            {
                MiniMapLayer = tryID;
            }
        }

        if (canvasRenderMode == MiniMapRenderMode.Mode3D)
        {
            Camera cam = Camera.main != null ? Camera.main : Camera.current;

            if (cam == null)
            {
                Debug.LogWarning("Main camera couldn't be found in the scene.");
                return;
            }

            m_Canvas.worldCamera = cam;
            cam.nearClipPlane = 0.015f;
            m_Canvas.planeDistance = 0.1f;
        }

        if (renderType == MiniMapRenderType.Picture)
        {
            miniMapCamera.cullingMask = 1 << MiniMapLayer;
        }

        if (excludeLayers.value != 0)
        {
            miniMapCamera.cullingMask &= ~excludeLayers.value;
        }

        Color bc = emptySpaceColor;
        bc.a *= backgroundOpacity;
        miniMapCamera.backgroundColor = bc;

        miniMapCamera.allowHDR = false;
        miniMapCamera.allowMSAA = false;

        miniMapCamera.enabled = cameraUpdateMode == MiniMapCameraUpdateMode.EveryFrame;
    }

    public void ConfigureWorldTarget()
    {
        if (m_Target == null)
        {
            return;
        }

        if (!m_Target.TryGetComponent<bl_MiniMapEntity>(out var mmi))
        {
            mmi = m_Target.AddComponent<bl_MiniMapEntity>();
        }

        MiniMapUI.ConfigureWorldTarget(mmi);
    }

    void Update()
    {
        if (hasError || m_Target == null || miniMapCamera == null)
        {
            return;
        }

        isUpdateFrame = Time.frameCount % UpdateRate == 0;

        if (cameraUpdateMode == MiniMapCameraUpdateMode.RateLimited)
        {
            miniMapCamera.Render();
        }

        if (!isMobile)
        {
            Inputs();
        }

        PositionControl();
        RotationControl();
        MapZoomControl();
        UpdateItems();
    }

    void PositionControl()
    {
        if (mapMode == MiniMapMapType.Local)
        {
            if (isUpdateFrame)
            {
                playerPosition = minimapRig.position;
                targetPosition = Target.position;

                playerPosition.x = targetPosition.x;

                if (!Ortographic2D)
                {
                    playerPosition.z = targetPosition.z;
                }
                else
                {
                    playerPosition.y = targetPosition.y;
                }

                playerPosition += DragOffset;

                if (Target != null && MiniMapUI.PlayerIconTransform != null)
                {
                    Vector3 pp = miniMapCamera.WorldToViewportPoint(targetPosition);
                    MiniMapUI.PlayerIconTransform.anchoredPosition = bl_MiniMapUtils.CalculateMiniMapPosition(pp, MiniMapUI.root);
                }

                if (!Ortographic2D)
                {
                    playerPosition.y = Target.TransformPoint(Vector3.up * 200).y;
                }
                else
                {
                    playerPosition.z = (targetPosition.z * 2) - (MaxZoom + (MinZoom * 0.5f));
                }
            }

            minimapRig.position = lerpTrackingPosition
                ? Vector3.Lerp(minimapRig.position, playerPosition, Time.deltaTime * 10)
                : playerPosition;
        }
    }

    void RotationControl()
    {
        if (DynamicRotation && mapMode != MiniMapMapType.Global)
        {
            if (isUpdateFrame)
            {
                playerRotation = minimapRig.eulerAngles;
                playerRotation.y = TargetRotation.y;
            }

            if (SmoothRotation)
            {
                if (isUpdateFrame)
                {
                    if (canvasRenderMode == MiniMapRenderMode.Mode2D)
                    {
                        MiniMapUI.PlayerIconTransform.eulerAngles = Vector3.zero;
                    }
                    else
                    {
                        MiniMapUI.PlayerIconTransform.localEulerAngles = Vector3.zero;
                    }
                }

                minimapRig.rotation = Quaternion.Slerp(
                    minimapRig.rotation,
                    Quaternion.Euler(playerRotation),
                    Time.smoothDeltaTime * LerpRotation
                );
            }
            else
            {
                minimapRig.eulerAngles = playerRotation;
            }
        }
        else
        {
            m_mapRotationOffsetVector.y = mapRotationOffset;
            minimapRig.eulerAngles = DeafultMapRot + m_mapRotationOffsetVector;

            if (canvasRenderMode == MiniMapRenderMode.Mode2D)
            {
                Vector3 e = Vector3.zero;
                e.z = -TargetRotation.y + mapRotationOffset;
                MiniMapUI.PlayerIconTransform.eulerAngles = e;
            }
            else
            {
                Vector3 tr = RotationTarget.localEulerAngles;
                Vector3 r = Vector3.zero;
                r.z = -tr.y;
                MiniMapUI.PlayerIconTransform.localEulerAngles = r;
            }
        }
    }

    void UpdateItems()
    {
        if (!isUpdateFrame)
        {
            return;
        }

        if (miniMapItems == null || miniMapItems.Count <= 0)
        {
            return;
        }

        for (int i = miniMapItems.Count - 1; i >= 0; i--)
        {
            if (miniMapItems[i] == null)
            {
                miniMapItems.RemoveAt(i);
                continue;
            }

            miniMapItems[i].OnUpdateItem();
        }
    }

    void Inputs()
    {
        if (inputHandler == null)
        {
            return;
        }

        if (inputHandler.IsInputDown(bl_MiniMapInputBase.MiniMapInput.ScreenMode))
        {
            ToggleSize();
        }

        if (inputHandler.IsInputDown(bl_MiniMapInputBase.MiniMapInput.ZoomOut))
        {
            ChangeZoom(true);
        }

        if (inputHandler.IsInputDown(bl_MiniMapInputBase.MiniMapInput.ZoomIn))
        {
            ChangeZoom(false);
        }
    }

    void MapZoomControl()
    {
        float zoom = Mathf.Lerp(miniMapCamera.orthographicSize, Zoom, Time.deltaTime * LerpHeight);
        zoom = Mathf.Max(1, zoom);
        miniMapCamera.orthographicSize = zoom;
    }

    void ToggleSize()
    {
        IsFullScreen = !IsFullScreen;

        if (IsFullScreen)
        {
            SetToFullscreenSize();
        }
        else
        {
            SetToMiniMapSize();
        }
    }

    public void SetToMiniMapSize()
    {
        IsFullScreen = false;

        if (FadeOnFullScreen)
        {
            MiniMapUI.DoStartFade(0.35f, null);
        }

        if (mapMode != MiniMapMapType.Global)
        {
            Zoom = minimapZoom;
        }

        if (mapShape != defaultShape)
        {
            mapShape = defaultShape;
        }

        MiniMapUI.minimapMaskManager?.ChangeMaskType(false);

        if (DynamicRotation != DefaultRotationMode)
        {
            DynamicRotation = DefaultRotationMode;
        }

        if (showCursorOnFullscreen)
        {
            Cursor.visible = wasCursorVisible;
            Cursor.lockState = wasCursorMode;
        }

        bl_MiniMapOverlay.Instance?.SetActive(IsFullScreen);

        if (ResetOffSetOnChange)
        {
            GoToTarget();
        }

        MiniMapUI.MiniMapSize?.DoTransition();

        if (pathNav != null)
        {
            pathNav.UpdateSize(this);
        }

        float ratio = GetViewportRatio();

        foreach (var item in miniMapItems)
        {
            if (item == null)
            {
                continue;
            }

            item.OnViewportChanged(ratio);
        }

        if (iconsSizeRelativeToZoom && MiniMapUI.playerIcon != null)
        {
            MiniMapUI.playerIcon.SetSize(playerIconSize * ratio);
        }
    }

    public void SetToFullscreenSize()
    {
        IsFullScreen = true;

        if (FadeOnFullScreen)
        {
            MiniMapUI.DoStartFade(0.35f, null);
        }

        if (mapMode != MiniMapMapType.Global)
        {
            Zoom = MaxZoom;
        }

        mapShape = MiniMapMapShape.Rectangle;
        MiniMapUI.minimapMaskManager?.ChangeMaskType(true);

        if (DynamicRotation)
        {
            DynamicRotation = false;
            ResetMapRotation();
        }

        if (showCursorOnFullscreen)
        {
            wasCursorVisible = Cursor.visible;
            wasCursorMode = Cursor.lockState;

            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        bl_MiniMapOverlay.Instance?.SetActive(IsFullScreen);

        if (ResetOffSetOnChange)
        {
            GoToTarget();
        }

        MiniMapUI.MiniMapSize?.DoTransition();

        if (pathNav != null)
        {
            pathNav.UpdateSize(this);
        }

        float ratio = GetViewportRatio();

        foreach (var item in miniMapItems)
        {
            if (item == null)
            {
                continue;
            }

            item.OnViewportChanged(ratio);
        }

        if (iconsSizeRelativeToZoom && MiniMapUI.playerIcon != null)
        {
            MiniMapUI.playerIcon.SetSize(playerIconSize * ratio);
        }
    }

    public void SetAsActiveMiniMap()
    {
        if (ActiveMiniMap == this)
        {
            return;
        }

        var othersMinimaps = FindObjectsByType<bl_MiniMap>(FindObjectsSortMode.None);

        for (int i = 0; i < othersMinimaps.Length; i++)
        {
            othersMinimaps[i].SetActive(false);
        }

        SetActive(true);

        if (ActiveMiniMap != null)
        {
            ActiveMiniMap.TransferIconsTo(this);
        }

        ActiveMiniMap = this;
        bl_MiniMapEvents.onActiveMiniMapChanged?.Invoke(this);
    }

    public void TransferIconsTo(bl_MiniMap otherMinimap)
    {
        foreach (var item in miniMapItems)
        {
            if (item == null)
            {
                continue;
            }

            item.ChangeMiniMapOwner(otherMinimap);

            if (!otherMinimap.miniMapItems.Contains(item))
            {
                otherMinimap.miniMapItems.Add(item);
            }
        }
    }

    public void SetActive(bool active, bool onlyUI = false)
    {
        if (!onlyUI)
        {
            gameObject.SetActive(active);

            if (miniMapPlane != null)
            {
                miniMapPlane.SetActive(active);
            }
        }
        else
        {
            MiniMapUI.SetActive(active);
        }
    }

    public void SetDragPosition(Vector3 pos)
    {
        if (DragOnlyOnFullScreen)
        {
            if (!IsFullScreen)
            {
                return;
            }
        }

        DragOffset.x += -pos.x * DragMovementSpeed.x;
        DragOffset.z += -pos.y * DragMovementSpeed.y;

        DragOffset.x = Mathf.Clamp(DragOffset.x, -MaxOffSetPosition.x, MaxOffSetPosition.x);
        DragOffset.z = Mathf.Clamp(DragOffset.z, -MaxOffSetPosition.y, MaxOffSetPosition.y);
    }

    public void SetPointMark(Vector3 Position)
    {
        if (!AllowMultipleMarks)
        {
            Destroy(mapPointer);
        }

        mapPointer = Instantiate(MapPointerPrefab, Position, Quaternion.identity) as GameObject;
        mapPointer.GetComponent<bl_MapPointerBase>().SetColor(playerColor);

        if (showPathNav)
        {
            if (pathNav == null)
            {
                var go = Instantiate(bl_MiniMapData.Instance.pathNavPrefab.gameObject, Vector3.zero, Quaternion.identity) as GameObject;
                pathNav = go.GetComponent<bl_MiniMapPathNav>();
                go.layer = LayerMask.NameToLayer("MiniMap");
                pathNav.SetColor(navPathColor);
                pathNav.SetWidth(navPathWidth);
            }

            pathNav.TrackTarget(Target, Position, this);
        }
    }

    public void GoToTarget()
    {
        StopCoroutine(nameof(ResetOffset));
        StartCoroutine(nameof(ResetOffset));
    }

    IEnumerator ResetOffset()
    {
        while (Vector3.Distance(DragOffset, Vector3.zero) > 0.2f)
        {
            DragOffset = Vector3.Lerp(DragOffset, Vector3.zero, Time.deltaTime * 12);
            yield return null;
        }

        DragOffset = Vector3.zero;
    }

    public void ChangeZoom(bool zoomIn)
    {
        if (mapMode == MiniMapMapType.Global)
        {
            return;
        }

        if (zoomIn)
        {
            Zoom += scrollSensitivity;
        }
        else
        {
            Zoom -= scrollSensitivity;
        }

        Zoom = Mathf.Clamp(Zoom, MinZoom, MaxZoom);
        minimapZoom = Zoom;

        if (saveZoomInRuntime)
        {
            PlayerPrefs.SetFloat(MMHeightKey, Zoom);
        }

        float ratio = GetViewportRatio();

        foreach (var item in miniMapItems)
        {
            if (item == null)
            {
                continue;
            }

            item.OnViewportChanged(ratio);
        }

        if (pathNav != null)
        {
            pathNav.UpdateSize(this);
        }

        if (iconsSizeRelativeToZoom && MiniMapUI.playerIcon != null)
        {
            MiniMapUI.playerIcon.SetSize(playerIconSize * ratio);
        }
    }

    public void DoHitEffect()
    {
        MiniMapUI?.DoHitEffect();
    }

    public bl_MiniMapEntityBase CreateNewItem(MiniMapIconSettings item)
    {
        if (hasError)
        {
            return null;
        }

        GameObject newItem = Instantiate(ItemPrefabSimple, item.Position, Quaternion.identity) as GameObject;
        var mmItem = newItem.GetComponent<bl_MiniMapEntityBase>();

        mmItem.SetIconSettings(item);

        return mmItem;
    }

    void ResetMapRotation()
    {
        minimapRig.eulerAngles = new Vector3(90, 0, 0);
    }

    public void ChangeMapSize(bool fullscreen)
    {
        IsFullScreen = fullscreen;
    }

    public void SetTarget(GameObject t)
    {
        m_Target = t;
    }

    public void SetTarget(bl_MiniMapTarget newTarget)
    {
        targetScript = newTarget;
    }

    public void SetMapTexture(Texture2D newTexture)
    {
        if (renderType != MiniMapRenderType.Picture)
        {
            Debug.LogWarning("You only can set texture in Picture Mode");
            return;
        }

        miniMapPlane.SetMapTexture(newTexture);
    }

#if UNITY_EDITOR
    public void OnValidate()
    {
        if (miniMapCamera != null)
        {
            miniMapCamera.orthographicSize = DefaultHeight;
        }

        if (MiniMapUI != null && MiniMapUI.playerIcon != null)
        {
            MiniMapUI.playerIcon.SetIcon(PlayerIconSprite, true);
            MiniMapUI.playerIcon.SetColor(playerColor);

            EditorUtility.SetDirty(MiniMapUI.playerIcon);
        }

        if (MiniMapUI != null)
        {
            if (MiniMapUI.rootAlpha != null)
            {
                MiniMapUI.rootAlpha.alpha = overallOpacity;
            }
        }
    }
#endif

    public void SetGridSize(float value)
    {
        if (miniMapPlane == null)
        {
            return;
        }

        miniMapPlane.SetGridSize(value);
    }

    public void SetActiveGrid(bool active)
    {
        if (miniMapPlane == null)
        {
            return;
        }

        miniMapPlane.SetActiveGrid(active);
    }

    public void SetMapRotationMode(bool dynamic)
    {
        if (IsFullScreen)
        {
            return;
        }

        DynamicRotation = dynamic;
        DefaultRotationMode = dynamic;
    }

    public void GetMiniMapSize()
    {
        var root = MiniMapUI.root;
        MiniMapSize = root.sizeDelta;
        MiniMapPosition = root.anchoredPosition;
        MiniMapRotation = root.eulerAngles;
    }

    public void GetFullMapSize()
    {
        var root = MiniMapUI.root;
        FullMapSize = root.sizeDelta;
        FullMapPosition = root.anchoredPosition;
        FullMapRotation = root.eulerAngles;
    }

    public void RegisterItem(bl_MiniMapEntityBase item)
    {
        if (miniMapItems.Contains(item))
        {
            return;
        }

        miniMapItems.Add(item);
    }

    public void RemoveItem(bl_MiniMapEntityBase item)
    {
        miniMapItems.Remove(item);
    }

    public float GetZoomRatio()
    {
        return DefaultHeight / Mathf.Max(Zoom, 1);
    }

    public float GetViewportRatio()
    {
        return MiniMapUI.MiniMapSize.GetSizeRatio() * GetZoomRatio();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void OnSceneLoad()
    {
        ActiveMiniMap = null;
    }

    public Transform Target
    {
        get
        {
            if (targetScript != null)
            {
                return targetScript.GetTarget();
            }

            return m_Target != null ? m_Target.transform : transform;
        }
        set
        {
            m_Target = value.gameObject;
        }
    }

    public Transform RotationTarget
    {
        get
        {
            if (rotationTargetOverride != null)
            {
                return rotationTargetOverride;
            }

            if (targetScript != null)
            {
                return targetScript.GetRotationTarget();
            }

            return m_Target != null ? m_Target.transform : transform;
        }
    }

    public Vector3 TargetRotation
    {
        get
        {
            Transform rotationTarget = RotationTarget;
            return rotationTarget != null ? rotationTarget.eulerAngles : Vector3.zero;
        }
    }

    public bool HasTarget()
    {
        return m_Target != null;
    }
}