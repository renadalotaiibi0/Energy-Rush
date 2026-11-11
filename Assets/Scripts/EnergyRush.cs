using UnityEngine;
using System.Collections.Generic;

public class EnergyRush : MonoBehaviour
{
    enum Mode { Menu, Playing, Lost, Won }
    Mode mode; int level, energy, hearts, total, score; float remaining, immunity, elapsed;
    readonly int[] needs={3,5,7}; readonly float[] limits={60,75,90};
    readonly string[] titles={"POWER UP","DANGER ZONE","FINAL CHARGE"};
    GameObject world,robot,visual,gate; CharacterController controller; Camera cam;
    Vector3 velocity; Vector3 spawn; bool help; float step;
    Material navy,cyan,white,yellow,red,dark,glow,blue;
    List<GameObject> cells=new List<GameObject>(); List<Transform> lasers=new List<Transform>();
    List<Vector3> laserOrigins=new List<Vector3>(); GUIStyle heading,label,small,button;
    AudioSource sound;
    void Awake(){
        Application.targetFrameRate=60;
        navy=Mat(new Color(.13f,.07f,.24f)); cyan=Mat(new Color(.13f,.65f,.83f)); white=Mat(new Color(.85f,.94f,1));
        yellow=Mat(new Color(1,.79f,.16f),true); red=Mat(new Color(1,.16f,.27f),true); dark=Mat(new Color(.055f,.10f,.19f)); glow=Mat(new Color(.12f,.92f,1),true); blue=Mat(new Color(.1f,.37f,.8f));
        RenderSettings.ambientLight=new Color(.42f,.44f,.65f); RenderSettings.fog=true; RenderSettings.fogColor=new Color(.07f,.035f,.15f); RenderSettings.fogDensity=.012f;
        var light=new GameObject("Station key light").AddComponent<Light>(); light.type=LightType.Directional; light.intensity=1.7f; light.transform.rotation=Quaternion.Euler(45,-25,0); light.shadows=LightShadows.Soft;
        cam=new GameObject("Follow camera").AddComponent<Camera>(); cam.tag="MainCamera"; cam.backgroundColor=RenderSettings.fogColor; cam.clearFlags=CameraClearFlags.SolidColor; cam.fieldOfView=58; cam.farClipPlane=240; cam.gameObject.AddComponent<AudioListener>();
        sound=gameObject.AddComponent<AudioSource>(); BuildLevel(0); mode=Mode.Menu;
    }
    Material Mat(Color c,bool emission=false){var m=new Material(Shader.Find("Standard"));m.color=c;if(emission){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",c*.7f);}return m;}
    GameObject Shape(string name,PrimitiveType type,Vector3 p,Vector3 s,Material m,Transform parent=null,bool collider=true){var o=GameObject.CreatePrimitive(type);o.name=name;o.transform.SetParent(parent==null?world.transform:parent,false);o.transform.localPosition=p;o.transform.localScale=s;o.GetComponent<Renderer>().sharedMaterial=m;if(!collider)Destroy(o.GetComponent<Collider>());return o;}
    Vector3 Point(int i){if(level==0)return new Vector3(0,0,i*8);if(level==1)return new Vector3(i<2?0:(i<4?5:0),0,i*8);return new Vector3(i%3==1?-5:(i%3==2?5:0),0,i*9);}
    void BuildLevel(int index){
        if(world!=null){world.SetActive(false);Destroy(world);} level=index;energy=0;hearts=3;remaining=limits[level];immunity=0;velocity=Vector3.zero;cells.Clear();lasers.Clear();laserOrigins.Clear();
        world=new GameObject("Level "+(level+1)+" - "+titles[level]);int n=needs[level];
        for(int i=0;i<=n+1;i++){
            Vector3 p=Point(i); Shape("Landing deck",PrimitiveType.Cube,p+Vector3.down*.45f,new Vector3(8,.9f,7),cyan);
            Shape("Deck inset",PrimitiveType.Cube,p+Vector3.up*.015f,new Vector3(6.9f,.035f,5.9f),dark);
            for(int side=-1;side<=1;side+=2){Shape("Luminous edge",PrimitiveType.Cube,p+new Vector3(side*3.7f,.07f,0),new Vector3(.12f,.12f,6.8f),glow);Shape("Support",PrimitiveType.Cube,p+new Vector3(side*3,-2,0),new Vector3(.5f,3,.5f),blue);}
            if(i>0){Vector3 a=Point(i-1),mid=(a+p)*.5f;float dist=Vector3.Distance(a,p);var bridge=Shape("Connecting corridor",PrimitiveType.Cube,mid+Vector3.down*.25f,new Vector3(4,.5f,dist),cyan);bridge.transform.rotation=Quaternion.LookRotation(p-a);}
            if(i>=1&&i<=n){var cell=Shape("Energy battery",PrimitiveType.Cylinder,p+new Vector3(0,1.05f,0),new Vector3(.65f,.6f,.65f),yellow,null,false);Shape("Battery cap",PrimitiveType.Cylinder,new Vector3(0,.57f,0),new Vector3(.55f,.14f,.55f),white,cell.transform,false);Shape("Energy band",PrimitiveType.Cube,new Vector3(0,0,-.51f),new Vector3(.45f,.5f,.06f),glow,cell.transform,false);cells.Add(cell);}
        }
        int hazards=level==0?1:level==1?3:5;
        for(int i=0;i<hazards;i++){Vector3 p=Point(i+1)+new Vector3(i%2==0?1.65f:-1.65f,.55f,2.25f);Shape("Red hazard",PrimitiveType.Cube,p,new Vector3(2.2f,1.1f,.7f),red).AddComponent<RushHazard>();}
        if(level==2)for(int i=0;i<3;i++){Vector3 p=Point(i*2+2)+new Vector3(0,.8f,-2.2f);var l=Shape("Moving laser",PrimitiveType.Cube,p,new Vector3(2.6f,.22f,.25f),red);l.AddComponent<RushHazard>();lasers.Add(l.transform);laserOrigins.Add(p);}
        Vector3 end=Point(n+1);gate=new GameObject("Energy gate");gate.transform.SetParent(world.transform);gate.transform.position=end;
        Shape("Left gate pillar",PrimitiveType.Cube,new Vector3(-2,1.7f,0),new Vector3(.5f,3.4f,.6f),white,gate.transform);Shape("Right gate pillar",PrimitiveType.Cube,new Vector3(2,1.7f,0),new Vector3(.5f,3.4f,.6f),white,gate.transform);Shape("Gate lintel",PrimitiveType.Cube,new Vector3(0,3.3f,0),new Vector3(4.5f,.5f,.6f),glow,gate.transform);Shape("Locked energy field",PrimitiveType.Cube,new Vector3(0,1.5f,0),new Vector3(3.5f,3,.1f),red,gate.transform,false);
        spawn=new Vector3(0,.15f,-1.8f);robot=new GameObject("Player robot");robot.transform.SetParent(world.transform);robot.transform.position=spawn;robot.AddComponent<RushRobotCollision>().game=this;controller=robot.AddComponent<CharacterController>();controller.height=1.7f;controller.radius=.37f;controller.center=new Vector3(0,.85f,0);controller.stepOffset=.3f;
        visual=new GameObject("Robot body");visual.transform.SetParent(robot.transform,false);
        Shape("Torso",PrimitiveType.Capsule,new Vector3(0,.85f,0),new Vector3(.75f,.45f,.5f),white,visual.transform,false);Shape("Head",PrimitiveType.Cube,new Vector3(0,1.43f,0),new Vector3(.82f,.59f,.6f),white,visual.transform,false);Shape("Face visor",PrimitiveType.Cube,new Vector3(0,1.46f,.315f),new Vector3(.66f,.29f,.045f),dark,visual.transform,false);
        for(int s=-1;s<=1;s+=2){Shape("Eye",PrimitiveType.Sphere,new Vector3(s*.18f,1.47f,.35f),new Vector3(.12f,.13f,.05f),glow,visual.transform,false);Shape("Arm",PrimitiveType.Capsule,new Vector3(s*.5f,.86f,0),new Vector3(.22f,.27f,.24f),blue,visual.transform,false);Shape("Boot",PrimitiveType.Cube,new Vector3(s*.23f,.25f,.05f),new Vector3(.3f,.4f,.48f),blue,visual.transform,false);}
        Shape("Antenna",PrimitiveType.Cylinder,new Vector3(0,1.85f,0),new Vector3(.06f,.15f,.06f),blue,visual.transform,false);Shape("Antenna light",PrimitiveType.Sphere,new Vector3(0,2.02f,0),Vector3.one*.16f,yellow,visual.transform,false);
        // Decorative station architecture, outside the playable decks.
        for(int i=0;i<12;i++){float z=i*8;Shape("Station tower",PrimitiveType.Cylinder,new Vector3(i%2==0?-14:14,-3,z),new Vector3(2,7+i%3,2),navy);Shape("Tower beacon",PrimitiveType.Sphere,new Vector3(i%2==0?-14:14,5+i%3,z),Vector3.one*.5f,glow,null,false);}
        cam.transform.position=spawn+new Vector3(0,6,-8);cam.transform.LookAt(robot.transform.position+Vector3.up);
    }
    void Update(){
        elapsed+=Time.deltaTime;
        foreach(var c in cells)if(c!=null){c.transform.Rotate(0,65*Time.deltaTime,0,Space.World);var p=c.transform.position;p.y=1.15f+Mathf.Sin(elapsed*2.8f)*.14f;c.transform.position=p;}
        if(mode!=Mode.Playing)return;
        remaining-=Time.deltaTime;immunity-=Time.deltaTime;if(remaining<=0){remaining=0;mode=Mode.Lost;Tone(140);return;}
        if(Input.GetKeyDown(KeyCode.Escape)){mode=Mode.Menu;return;}
        Vector3 input=new Vector3(Input.GetAxisRaw("Horizontal"),0,Input.GetAxisRaw("Vertical")).normalized;
        if(controller.isGrounded&&velocity.y<0)velocity.y=-2;
        if(controller.isGrounded&&Input.GetKeyDown(KeyCode.Space)){velocity.y=7.4f;Tone(520);}
        velocity.y+=-20*Time.deltaTime;controller.Move((input*6+velocity)*Time.deltaTime);
        if(input.sqrMagnitude>.01f){visual.transform.rotation=Quaternion.Slerp(visual.transform.rotation,Quaternion.LookRotation(input),12*Time.deltaTime);step+=Time.deltaTime*12;}
        visual.transform.localPosition=new Vector3(0,Mathf.Sin(step)*.035f,0);visual.SetActive(immunity<=0||Mathf.FloorToInt(elapsed*14)%2==0);
        for(int i=cells.Count-1;i>=0;i--)if(Vector3.Distance(robot.transform.position+Vector3.up,cells[i].transform.position)<1.15f){Destroy(cells[i]);cells.RemoveAt(i);energy++;total++;score+=100;Tone(700+energy*70);if(energy==needs[level])gate.transform.Find("Locked energy field").GetComponent<Renderer>().sharedMaterial=glow;}
        for(int i=0;i<lasers.Count;i++)lasers[i].position=laserOrigins[i]+Vector3.right*Mathf.Sin(elapsed*1.6f+i)*2.1f;
        if(robot.transform.position.y<-5){Damage();controller.enabled=false;robot.transform.position=spawn;controller.enabled=true;velocity=Vector3.zero;}
        if(energy==needs[level]&&Vector3.Distance(robot.transform.position,gate.transform.position)<2.1f){score+=Mathf.CeilToInt(remaining)*10;if(level==2){mode=Mode.Won;Tone(1000);}else BuildLevel(level+1);}
    }
    void LateUpdate(){if(robot==null)return;Vector3 target=robot.transform.position+new Vector3(0,6.2f,-8.2f);cam.transform.position=Vector3.Lerp(cam.transform.position,target,1-Mathf.Exp(-7*Time.deltaTime));cam.transform.LookAt(robot.transform.position+new Vector3(0,1.1f,2));}
    public void Damage(){if(mode!=Mode.Playing||immunity>0)return;hearts--;immunity=1.5f;Tone(180);if(hearts<=0)mode=Mode.Lost;}
    void Tone(float hz){int count=4410;var clip=AudioClip.Create("Robot tone",count,1,44100,false);float[] data=new float[count];for(int i=0;i<count;i++)data[i]=Mathf.Sin(i*hz*2*Mathf.PI/44100)*.12f*(1-i/(float)count);clip.SetData(data,0);sound.PlayOneShot(clip);Destroy(clip,.3f);}
    void MenuAction(string action){if(action=="HOW TO PLAY")help=!help;else if(action=="MAIN MENU"){mode=Mode.Menu;BuildLevel(0);}else if(action=="PLAY"||action=="TRY AGAIN"||action=="PLAY AGAIN")Begin();}
    void Begin(){score=0;total=0;BuildLevel(0);mode=Mode.Playing;}
    void Styles(){if(heading!=null)return;heading=new GUIStyle(GUI.skin.label){fontSize=48,fontStyle=FontStyle.Bold,alignment=TextAnchor.MiddleCenter};heading.normal.textColor=Color.white;label=new GUIStyle(heading){fontSize=23};small=new GUIStyle(label){fontSize=16};button=new GUIStyle(GUI.skin.button){fontSize=21,fontStyle=FontStyle.Bold};button.normal.textColor=Color.white;}
    void OnGUI(){Styles();float scale=Mathf.Min(Screen.width/1100f,Screen.height/720f);GUI.matrix=Matrix4x4.TRS(Vector3.zero,Quaternion.identity,new Vector3(scale,scale,1));float w=Screen.width/scale,h=Screen.height/scale;
        if(mode==Mode.Playing){Panel(new Rect(22,20,w-44,88),new Color(.035f,.045f,.12f,.94f));GUI.Label(new Rect(40,28,270,32),"ENERGY RUSH",label);GUI.Label(new Rect(40,64,270,26),"0"+(level+1)+" / "+titles[level],small);GUI.Label(new Rect(w/2-155,35,310,40),"ENERGY  "+energy+" / "+needs[level],label);GUI.Label(new Rect(w-320,30,270,32),"TIME  "+Mathf.CeilToInt(remaining)+"s",label);GUI.Label(new Rect(w-320,67,270,30),"LIFE  "+new string('♥',hearts),small);return;}
        Panel(new Rect(0,0,w,h),new Color(.035f,.025f,.11f,.86f));float x=w/2-270;GUI.Label(new Rect(x,90,540,80),mode==Mode.Menu?"ENERGY RUSH":mode==Mode.Won?"YOU WIN!":"GAME OVER",heading);
        GUI.Label(new Rect(x,175,540,50),mode==Mode.Menu?"A LITTLE ROBOT. A BIG MISSION.":"SCORE  "+score+"     •     BATTERIES  "+total+" / 15",label);
        if(mode==Mode.Menu){GUI.Label(new Rect(x,235,540,50),"Restore the station. Charge all three gates.",small);if(GUI.Button(new Rect(x+100,310,340,60),"PLAY",button))MenuAction("PLAY");if(GUI.Button(new Rect(x+100,385,340,55),help?"HIDE INSTRUCTIONS":"HOW TO PLAY",button))MenuAction("HOW TO PLAY");if(help)GUI.Label(new Rect(x-70,460,680,130),"WASD / ARROWS to move • SPACE to jump\nCollect every yellow battery to unlock the portal.\nRed obstacles cost a heart. Falls return you to the start.\nThree hearts per level. Beat the clock!",small);}
        else{GUI.Label(new Rect(x,240,540,45),mode==Mode.Won?"Station restored. Outstanding work, little robot!":"Recharge and give it another run.",small);if(GUI.Button(new Rect(x+100,320,340,60),mode==Mode.Won?"PLAY AGAIN":"TRY AGAIN",button))MenuAction(mode==Mode.Won?"PLAY AGAIN":"TRY AGAIN");if(GUI.Button(new Rect(x+100,400,340,55),"MAIN MENU",button))MenuAction("MAIN MENU");}
        if(mode==Mode.Won)for(int i=0;i<40;i++){float px=(i*97)%Mathf.Max(1,w);float py=(elapsed*(45+i%5*15)+i*47)%h;Panel(new Rect(px,py,8,14),i%2==0?Color.cyan:Color.yellow);}
    }
    void Panel(Rect rect,Color color){Color old=GUI.color;GUI.color=color;GUI.DrawTexture(rect,Texture2D.whiteTexture);GUI.color=old;}
}
public class RushHazard:MonoBehaviour { }
public class RushRobotCollision:MonoBehaviour { public EnergyRush game; void OnControllerColliderHit(ControllerColliderHit hit){if(hit.collider.GetComponent<RushHazard>()!=null)game.Damage();} }
