using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//using UnityEngine.EventSystems;

public class MJ : MonoBehaviour
{
    private RaycastHit ObjHit;
    private Ray CustomRay;

    private bool isSelect = false;

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

        if (Input.GetMouseButtonDown(0))
        {
            CustomRay = Camera.main.ScreenPointToRay(Input.mousePosition); //！！！注意，射线对碰撞体才有效，需要加collider组件
                                                                           //显示一条射线，只有在scene视图中才能看到
            Debug.DrawLine(CustomRay.origin, CustomRay.direction, Color.red, 2);
            if (Physics.Raycast(CustomRay, out ObjHit, 300))
            {
                if (ObjHit.collider.gameObject != null && ObjHit.collider.gameObject.name == gameObject.name)
                {
                    //print(ObjHit.collider.gameObject.name + "====="); 
                    print("Click Object: 碰撞的是" + ObjHit.collider.gameObject.name + "isSelect=" + (isSelect ? "true" : "false") + "|当前" + gameObject.name + "|" + MainManager._instance.dictWhole[System.Convert.ToInt32(ObjHit.collider.gameObject.name)].ToString() + "|位置：" + Input.mousePosition.x + "，" + Input.mousePosition.y + "，" + Input.mousePosition.z);
                    MainManager._instance.Click(ObjHit.collider.gameObject, isSelect);
                }
            }
        }
    }

    //private void OnMouseDown()
    //{
    //    print("OnMouseDown"+gameObject.transform.name);
    //}

    public void Select(/*GameObject gameObject*/)
    {
        //print("select");
        if (!isSelect)
        {
            Vector3 currentPos = gameObject.transform.position;
            gameObject.transform.position = new Vector3(currentPos.x, currentPos.y + 2, currentPos.z);
            isSelect = true;
        }
        //else
        //{
        //    Vector3 currentPos = gameObject.transform.position;
        //    gameObject.transform.position = new Vector3(currentPos.x, currentPos.y - 2, currentPos.z);
        //    isSelect = false;
        //}
    }

    public void ResetSelect()
    {
        if (isSelect)
        {
            Vector3 currentPos = gameObject.transform.position;
            gameObject.transform.position = new Vector3(currentPos.x, currentPos.y - 2, currentPos.z);
            isSelect = false;
        }
    }
}
