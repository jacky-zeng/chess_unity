using UnityEngine;

public class MJOnline : MonoBehaviour
{
    private RaycastHit ObjHit;
    private Ray CustomRay;

    private bool isSelect = false;

    private bool isDragging = false;  //是否在拖动中
    private Vector3 startPosition;    //开始时的坐标
    private Vector3 offset;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        //if (Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
        //{
        //    select();
        //}

        if (isDragging)
        {
            Vector3 newPosition = new Vector3(Input.mousePosition.x, Input.mousePosition.y, Camera.main.WorldToScreenPoint(transform.position).z);
            transform.position = Camera.main.ScreenToWorldPoint(newPosition) + offset;

            //CustomRay = Camera.main.ScreenPointToRay(Input.mousePosition); //！！！注意，射线对碰撞体才有效，需要加collider组件
        }

        //if (Input.GetMouseButtonDown(0))
        //{
        //    CustomRay = Camera.main.ScreenPointToRay(Input.mousePosition); //！！！注意，射线对碰撞体才有效，需要加collider组件
        //                                                                   //显示一条射线，只有在scene视图中才能看到
        //    Debug.DrawLine(CustomRay.origin, CustomRay.direction, Color.red, 2);
        //    if (Physics.Raycast(CustomRay, out ObjHit, 300))
        //    {
        //        if (ObjHit.collider.gameObject != null && ObjHit.collider.gameObject.name == gameObject.name)
        //        {
        //            //print(ObjHit.collider.gameObject.name + "====="); 
        //            //print("Click Object: 碰撞的是" + ObjHit.collider.gameObject.name + "isSelect=" + (isSelect ? "true" : "false") + "|当前" + gameObject.name + "|" + MainOnlineManager._instance.dictWhole[System.Convert.ToInt32(ObjHit.collider.gameObject.name)].ToString() + "|位置：" + Input.mousePosition.x + "，" + Input.mousePosition.y + "，" + Input.mousePosition.z);
        //            MainOnlineManager._instance.Click(ObjHit.collider.gameObject, isSelect);
        //        }
        //    }
        //}
    }

    // 当鼠标按下时开始拖动
    private void OnMouseDown()
    {
        CustomRay = Camera.main.ScreenPointToRay(Input.mousePosition); //！！！注意，射线对碰撞体才有效，需要加collider组件
                                                                       //显示一条射线，只有在scene视图中才能看到
        //Debug.DrawLine(CustomRay.origin, CustomRay.direction, Color.red, 2);
        if (Physics.Raycast(CustomRay, out ObjHit, 300))
        {
            if (ObjHit.collider.gameObject != null && ObjHit.collider.gameObject.name == gameObject.name)
            {
                startPosition = transform.position;
                if (startPosition.y < 30)
                {
                    startPosition.y = startPosition.y + 2;
                }

                isDragging = true;
                offset = transform.position - Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, Camera.main.WorldToScreenPoint(transform.position).z));
            }
        }
    }

    // 当鼠标抬起时停止拖动
    private void OnMouseUp()
    {
        //Debug.DrawLine(CustomRay.origin, CustomRay.direction, Color.red, 2);
        if (isDragging && ObjHit.collider.gameObject != null && ObjHit.collider.gameObject.name == gameObject.name)
        {
            isDragging = false;
            //print(ObjHit.collider.gameObject.name + "====="); 
            //print("Click Object: 碰撞的是" + ObjHit.collider.gameObject.name + "isSelect=" + (isSelect ? "true" : "false") + "|当前" + gameObject.name + "|" + MainOnlineManager._instance.dictWhole[System.Convert.ToInt32(ObjHit.collider.gameObject.name)].ToString() + "|位置：" + Input.mousePosition.x + "，" + Input.mousePosition.y + "，" + Input.mousePosition.z);
            print(transform.position.y + "=====transform.position.y");
            isSelect = transform.position.y >= 39 ? true : (transform.position.y < 30 ? false : isSelect); //拖动超出界限，直接认为是要打出
            MainOnlineManager._instance.Click(ObjHit.collider.gameObject, isSelect);
        }
    }

    public void Select(/*GameObject gameObject*/)
    {
        print(transform.position.y + "=====transform.position.y isSelect = " + isSelect);
        //print("select");
        if (!isSelect)
        {
            Vector3 currentPos = startPosition;
            transform.position = new Vector3(currentPos.x, currentPos.y + (currentPos.y >= 30 ? 0 : 2), currentPos.z);
            isSelect = true;
        }
        else
        {
            transform.position = startPosition;
        }
    }

    //public void ResetSelect()
    //{
    //    if (isSelect)
    //    {
    //        Vector3 currentPos = startPosition;
    //        transform.position = new Vector3(currentPos.x, currentPos.y - (currentPos.y >= 30 ? 0 : 2), currentPos.z);
    //        isSelect = false;
    //    }
    //}

    //牌弹起显示
    public void StandUp()
    {
        Vector3 currentPos = transform.position;
        transform.position = new Vector3(currentPos.x, currentPos.y + (currentPos.y >= 30 ? 0 : 2), currentPos.z);
    }

}