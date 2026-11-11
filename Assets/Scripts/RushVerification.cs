using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
public class RushVerification:MonoBehaviour {
 EnergyRush game; int errors; string output; List<string> checks=new List<string>();
 static BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
 object Get(string name)=>typeof(EnergyRush).GetField(name,flags).GetValue(game);
 void Set(string name,object value)=>typeof(EnergyRush).GetField(name,flags).SetValue(game,value);
 void Call(string name,params object[] args)=>typeof(EnergyRush).GetMethod(name,flags).Invoke(game,args);
 void Check(bool value,string text){checks.Add((value?"PASS ":"FAIL ")+text);Debug.Log(checks[checks.Count-1]);}
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void Boot(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"--energy-rush-test")>=0)new GameObject("Verification").AddComponent<RushVerification>();}
 void OnEnable(){Application.logMessageReceived+=Log;}
 void OnDisable(){Application.logMessageReceived-=Log;}
 void Log(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors++;}
 IEnumerator Capture(string name){ScreenCapture.CaptureScreenshot(Path.Combine(output,name+".png"));yield return new WaitForSeconds(.2f);}
 IEnumerator Start(){output=Path.GetFullPath(Path.Combine(Application.dataPath,"../../ReleaseChecks"));Directory.CreateDirectory(output);yield return null;Check(!Debug.isDebugBuild,"release player: Development Build disabled");game=FindAnyObjectByType<EnergyRush>();Check(game!=null,"game initialized");Check(Get("mode").ToString()=="Menu","start menu");yield return Capture("StartScreen");Call("MenuAction","HOW TO PLAY");Check((bool)Get("help"),"HOW TO PLAY action");Call("MenuAction","HOW TO PLAY");Call("MenuAction","PLAY");yield return null;Check(Get("mode").ToString()=="Playing","PLAY action");yield return Capture("Gameplay");
 var robot=(GameObject)Get("robot");var cc=robot.GetComponent<CharacterController>();Vector3 before=robot.transform.position;cc.Move(Vector3.forward);Check(robot.transform.position.z>before.z+.8f,"controller movement");
 // Contact an actual red obstacle, exercising OnControllerColliderHit.
 var hazard=FindAnyObjectByType<RushHazard>();cc.enabled=false;robot.transform.position=hazard.transform.position+Vector3.back*1.5f-Vector3.up*.5f;cc.enabled=true;Physics.SyncTransforms();cc.Move(Vector3.forward*1.5f);Check((int)Get("hearts")==2,"obstacle collision removes heart");game.Damage();Check((int)Get("hearts")==2,"damage immunity");
 for(int l=0;l<3;l++){
  yield return Capture("Level"+(l+1)+"Playing");Check((int)Get("level")==l,"level "+(l+1));Check(Mathf.Abs((float)Get("remaining")-new float[]{60,75,90}[l])<3,"level timer");
  var gate=(GameObject)Get("gate");Teleport(gate.transform.position);yield return null;Check((int)Get("level")==l&&Get("mode").ToString()=="Playing","uncollected gate stays locked");
  var cells=(List<GameObject>)Get("cells");int count=cells.Count;Check(count==new int[]{3,5,7}[l],"required batteries");
  while(cells.Count>0){Teleport(cells[0].transform.position-Vector3.up);yield return null;}
  Check((int)Get("energy")==count,"battery collection");yield return Capture("Level"+(l+1)+"Charged");if(l==2){var beams=(List<Transform>)Get("lasers");Vector3 old=beams[0].position;yield return new WaitForSeconds(.2f);Check((beams[0].position-old).sqrMagnitude>.001f,"moving laser animates");}Teleport(gate.transform.position);yield return null;
 }
 Check(Get("mode").ToString()=="Won","third gate wins");Check((int)Get("total")==15,"all 15 batteries");yield return Capture("YouWin");Call("MenuAction","PLAY AGAIN");Check(Get("mode").ToString()=="Playing"&&(int)Get("level")==0,"PLAY AGAIN action");Set("remaining",.001f);yield return null;Check(Get("mode").ToString()=="Lost","timer game over");yield return Capture("GameOver");Call("MenuAction","MAIN MENU");Check(Get("mode").ToString()=="Menu","MAIN MENU action");Call("MenuAction","PLAY");for(int i=0;i<3;i++){Set("immunity",0f);game.Damage();}Check(Get("mode").ToString()=="Lost","three hearts game over");Call("MenuAction","TRY AGAIN");Check(Get("mode").ToString()=="Playing","TRY AGAIN action");Check((int)Get("hearts")==3&&(int)Get("score")==0,"restart reset");
 Check(errors==0,"no runtime errors/exceptions/asserts");File.WriteAllLines(Path.Combine(output,"Verification.txt"),checks);Application.Quit(checks.Exists(s=>s.StartsWith("FAIL"))?1:0);
 }
 void Teleport(Vector3 p){var cc=(CharacterController)Get("controller");cc.enabled=false;cc.transform.position=p;cc.enabled=true;Set("velocity",Vector3.zero);Physics.SyncTransforms();}
}
