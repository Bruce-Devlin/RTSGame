using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerRig : MonoBehaviour
{
    #region Variables
    public bool useFor = false;
    public FollowableObject followObject;
    public Bean possessedBean = null;
    public List<Bean> selectedUnits;

    public Color teamColour;

    public List<Bean> allUnits;
    public static Camera playerCamera = null;
    public float maxCameraHeight = 500f;
    public float minCameraHeight = 10f;
    static float currentCameraHeight()
    {
        if (playerCamera != null)
        {
            float val = playerCamera.transform.localPosition.y;
            if (val < 0) return 1;
            else return val;
        }
        else return 1;
    }

    public bool canMove = true;
    float movementSpeed = 0f;
    public float normalMovementSpeed = 1f;
    public float fastMovementSpeed = 2f;
    public float slowMovementSpeed = 0.5f;
    public float movementTime = 5f;
    public float rotationAmount = 1f;
    public Vector3 zoomAmount;

    public Vector3 dragStartPosition;
    public Vector3 dragCurrentPostion;

    public Vector3 rotationStartPostion;
    public Vector3 rotationCurrentPosition;

    public bool colliding = false;

    Vector3 newPosition;
    Vector3 newBeanPosition;
    Quaternion newRotation;
    public Vector3 newZoom;

    public RectTransform selectionBox;
    private Vector2 selectionStartPos;
    private bool selecting = false;

    public GameObject beanPrefab;
    #endregion

    #region Unity Methods
    private void Awake() 
    {
        playerCamera = this.GetComponentInChildren<Camera>();
        
        newPosition = transform.position;
        newRotation = transform.rotation;
        
        newZoom = playerCamera.transform.localPosition;
        allUnits.RemoveAll(delegate (Bean unit) { return unit == null; });
        
        foreach (Bean bean in allUnits)
        {
            bean.playerRig = this;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        selectionStartPos = Input.mousePosition;
    }
    
    // Update is called once per frame
    void Update()
    {
        if (canMove)
        {
            Vector3 mouse = Input.mousePosition;
            Ray castPoint = Camera.main.ScreenPointToRay(mouse);
            RaycastHit hit;
            if (Physics.Raycast(castPoint, out hit))
            {
                HandleCommandKeys(hit);
                HandleUnitSelection(hit);
                HandleMouseInput(hit);
                HandleKeyInput();
            }
            
            HandleMovement();
        }

        //Dev
        if (Input.GetKey(KeyCode.F1))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
        if (Input.GetKeyDown(KeyCode.F2))
        {
            GameObject newBean = Instantiate(beanPrefab, transform.position, transform.rotation);
            Bean bean = newBean.GetComponent<Bean>();
            bean.playerRig = this;
            allUnits.Add(bean);
        }
        if (Input.GetKeyDown(KeyCode.F3))
        {
            GameObject newBean = Instantiate(beanPrefab, transform.position, transform.rotation);
            Bean bean = newBean.GetComponent<Bean>();
            bean.teamColor = Color.red;
        }
    }
    #endregion
    #region Handlers
    void HandleCommandKeys(RaycastHit hit)
    {
        if (followObject != null)
        {
            if (Input.GetKeyDown(KeyCode.T))
            {
                possessedBean = followObject.transform.parent.GetComponent<Bean>();
                if (!possessedBean.possessed) possessedBean.possessed = true;
            }
        }

        if (possessedBean != null)
        {
            newBeanPosition = possessedBean.transform.position;
            if (Input.GetKeyUp(KeyCode.Escape))
            {
                possessedBean.possessed = false;
                possessedBean = null;
            }
        }
        else
        {
            if (Input.GetMouseButton(1))
            {
                if (hit.transform.gameObject.tag == "BeanCircle" || hit.transform.gameObject.tag == "Bean")
                {
                    Bean selectedBean = hit.transform.GetComponentInParent<Bean>();
                    if (selectedUnits.Contains(selectedBean))
                    {
                        Vector3 target = hit.point;
                        selectedBean.Move(target);
                    }
                }
            }

            //Bean Move Order
            if (Input.GetMouseButtonDown(1))
            {
                if (selectedUnits.Count > 0 && hit.transform.gameObject.tag == "Ground" && hit.transform.gameObject.tag != "BeanCircle")
                {
                    if (useFor)
                    {
                        for (int i = 0; i < selectedUnits.Count; i++)
                        {
                            selectedUnits[i].Move(hit.point);
                        }
                    }
                    else
                    {
                        foreach(Bean bean in selectedUnits)
                        {   
                            bean.Move(hit.point);
                        }
                    }
                    
                }
            }

            if (Input.GetKeyDown(KeyCode.G) && selectedUnits.Count > 1)
            {                
                foreach (Bean bean in selectedUnits)
                {
                    //group beans
                    bean.grouped = true;
                    bean.groupedBeans = selectedUnits;
                    bean.groupLeader = selectedUnits[0];
                    if (bean != bean.groupLeader)
                    {
                        bean.drawNavPath.visible = false;
                    }
                }
            }
        }
    }

    void HandleUnitSelection(RaycastHit hit)
    {
        if (possessedBean == null)
        {
            //Single-select
            if (Input.GetMouseButtonDown(0) && !Input.GetKey(KeyCode.LeftControl))
            {
                if (hit.transform.gameObject.tag == "Bean" && allUnits.Contains(hit.transform.parent.GetComponent<Bean>()))
                {
                    selectedUnits.Clear();
                    Bean bean = hit.transform.parent.GetComponent<Bean>();

                    if (bean.grouped) bean = bean.groupLeader;

                    selectedUnits.Add(bean);
                }
                else selectedUnits.Clear();
            }
            //Multi-select
            else if (Input.GetMouseButtonDown(0) && Input.GetKey(KeyCode.LeftControl))
            {
                if (hit.transform.gameObject.tag == "Bean" && allUnits.Contains(hit.transform.parent.GetComponent<Bean>()))
                {
                    Bean bean = hit.transform.parent.GetComponent<Bean>();

                    if (bean.grouped) bean = bean.groupLeader;

                    if (!selectedUnits.Contains(bean)) 
                    {
                        selectedUnits.Add(bean);
                    }
                    else
                    {
                        selectedUnits.Remove(bean);
                    } 
                }
                else selectedUnits.Clear();
            }

            /*
            //Select All Unit
            if (Input.GetKeyDown(KeyCode.Space))
            {
                selectedUnits = allUnits;
            }
            */

            if (Input.GetKey(KeyCode.LeftShift))
            {
                //Get Selection Box start position
                if (Input.GetMouseButtonDown(0))
                {
                    selectionStartPos = Input.mousePosition;
                }

                //Start Box Selection
                if (Input.GetMouseButton(0))
                {
                    selecting = true;
                    UpdateSelectionBox(Input.mousePosition);
                }
            }

            //Stop Box Selection
            if (Input.GetMouseButtonUp(0) && selecting)
            {
                selecting = false;
                ReleaseSelectionBox();
            }  
        }
        
    }

    void HandleMouseInput(RaycastHit hit)
    {
        //Zoom
        if (Input.mouseScrollDelta.y != 0)
        {
            newZoom += Input.mouseScrollDelta.y * zoomAmount;
        }

        if (followObject == null)
        {
            //Movement       
            if (Input.GetMouseButtonDown(2) && hit.transform.gameObject.tag == "Ground" && !Input.GetKey(KeyCode.LeftAlt))
            {
                Plane plane = new Plane(Vector3.up, Vector3.zero); 

                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

                float entry;

                if(plane.Raycast(ray, out entry))
                {
                    dragStartPosition = ray.GetPoint(entry);
                }
            }     
            if (Input.GetMouseButton(2) && hit.transform.gameObject.tag == "Ground" && !Input.GetKey(KeyCode.LeftAlt))
            {
                Plane plane = new Plane(Vector3.up, Vector3.zero); 

                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

                float entry;

                if(plane.Raycast(ray, out entry))
                {
                    dragCurrentPostion = ray.GetPoint(entry);

                    newPosition = transform.position + dragStartPosition - dragCurrentPostion;
                }
            }
        }
        else 
        {
            newPosition = followObject.transform.position;

            if (Input.GetMouseButtonDown(0))
            {
                if (hit.transform.GetComponent<FollowableObject>() != followObject && possessedBean == null)
                {
                    followObject = null;
                }
                
            }
        }   

        //Rotation
        if (Input.GetMouseButtonDown(2) && Input.GetKey(KeyCode.LeftAlt))
        {
            rotationStartPostion = Input.mousePosition;
        }
        if (Input.GetMouseButton(2) && Input.GetKey(KeyCode.LeftAlt))
        {
            rotationCurrentPosition = Input.mousePosition;

            Vector3 difference = rotationStartPostion - rotationCurrentPosition;

            rotationStartPostion = rotationCurrentPosition;

            newRotation *= Quaternion.Euler(Vector3.up * (-difference.x / 5f));
        }
    }

    void HandleKeyInput()
    {
        //Sprinting?
        if (Input.GetKey(KeyCode.LeftShift))
        {
            movementSpeed = fastMovementSpeed;
        } 
        else if (Input.GetKey(KeyCode.LeftControl))
        {
            movementSpeed = slowMovementSpeed;
        }
        else movementSpeed = normalMovementSpeed;

        //Zoom?
        if (Input.GetKey(KeyCode.R))
        {
            newZoom += zoomAmount / 2;
        }
        if (Input.GetKey(KeyCode.F))
        {
            newZoom -= zoomAmount / 2;
        }

        if (possessedBean == null && followObject == null)
        {
            //WASD Movement
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            {
                newPosition += (transform.forward * movementSpeed);
            }
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            {
                newPosition += (transform.right * -movementSpeed);
            }
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            {
                newPosition += (transform.forward * -movementSpeed);
            }
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            {
                newPosition += (transform.right * movementSpeed);
            }
        }
        else if (possessedBean != null)
        {
            //WASD Movement
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            {
                newBeanPosition += (possessedBean.transform.forward * (possessedBean.beanSpeed / 2));
            }
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            {
                newBeanPosition += (possessedBean.transform.right * -(possessedBean.beanSpeed / 2));
            }
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            {
                newBeanPosition += (possessedBean.transform.forward * -(possessedBean.beanSpeed / 2));
            }
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            {
                newBeanPosition += (possessedBean.transform.right * (possessedBean.beanSpeed / 2));
            }
        }

        if (Input.GetKey(KeyCode.Q))
        {
            newRotation *= Quaternion.Euler(Vector3.up * (rotationAmount));
        }    
        if (Input.GetKey(KeyCode.E))
        {
            newRotation *= Quaternion.Euler(Vector3.up * -(rotationAmount));
        }
    }

    void HandleMovement()
    {
        if (transform.position != newPosition) transform.position = Vector3.Lerp(transform.position, newPosition, Time.deltaTime * (movementTime * Mathf.Log(currentCameraHeight() / 2)));
        if (transform.rotation != newRotation) transform.rotation = Quaternion.Lerp(transform.rotation, newRotation, Time.deltaTime * (movementTime * Mathf.Log(currentCameraHeight() / 2)));
        if (possessedBean != null) possessedBean.Move(newBeanPosition);

        
        if (playerCamera.transform.localPosition != newZoom) 
        {
            if (newZoom.y <= minCameraHeight)
            {
                newZoom.y = minCameraHeight;
            }
            if (newZoom.y >= maxCameraHeight)
            {
                newZoom.y = maxCameraHeight;
            }

            newZoom.z = -newZoom.y;
            playerCamera.transform.localPosition = Vector3.Lerp(playerCamera.transform.localPosition, newZoom, Time.deltaTime * (movementTime * Mathf.Log(currentCameraHeight()) / 2));
        }
    }
    #endregion
    #region Helpers
    void UpdateSelectionBox (Vector2 curMousePos)
    {
        if(!selectionBox.gameObject.activeInHierarchy)
            selectionBox.gameObject.SetActive(true);
        
        float width = curMousePos.x - selectionStartPos.x;
        float height = curMousePos.y - selectionStartPos.y;

        selectionBox.sizeDelta = new Vector2(Mathf.Abs(width), Mathf.Abs(height));
        selectionBox.anchoredPosition = selectionStartPos + new Vector2(width / 2, height / 2);
    }

    // called when we release the selection box
    void ReleaseSelectionBox ()
    {
        if (selectionBox.sizeDelta.x > 2 || selectionBox.sizeDelta.y > 2)
        {
            selectionBox.gameObject.SetActive(false);
            Vector2 min = selectionBox.anchoredPosition - (selectionBox.sizeDelta / 2);
            Vector2 max = selectionBox.anchoredPosition + (selectionBox.sizeDelta / 2);

            for (int i = 0; i < allUnits.Count; i++)
            {
                Bean bean = allUnits[i];

                Vector3 screenPos = playerCamera.transform.GetComponent<Camera>().WorldToScreenPoint(bean.transform.position);
                
                if(screenPos.x > min.x && screenPos.x < max.x && screenPos.y > min.y && screenPos.y < max.y)
                {
                    selectedUnits.Add(bean);
                }
            }
        }
        
    }
    #endregion
}
