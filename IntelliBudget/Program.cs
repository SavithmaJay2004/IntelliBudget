using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;
namespace IntelliBudget {
static class Program {
 [STAThread] static void Main(){Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
  while(true){using(var login=new LoginForm()){
   if(login.ShowDialog()!=DialogResult.OK)return;
   using(var main=new DashboardForm(login.UserId,login.Username)){Application.Run(main);if(!main.SignedOut)return;}
  }}
 }
}
static class UI {
 public static readonly Color Background=Color.FromArgb(14,20,33);
 public static readonly Color Surface=Color.FromArgb(23,33,51);
 public static readonly Color Raised=Color.FromArgb(31,45,66);
 public static readonly Color Border=Color.FromArgb(47,63,86);
 public static readonly Color Text=Color.FromArgb(240,245,253);
 public static readonly Color Muted=Color.FromArgb(167,185,208);
 public static readonly Color Accent=Color.FromArgb(112,235,199);
 public static readonly Color Blue=Color.FromArgb(133,175,255);
 public static readonly Color Amber=Color.FromArgb(250,196,116);

 public static void Style(Form form){
  form.BackColor=Background;
  form.ForeColor=Text;
  form.Font=new Font("Segoe UI",10);
  form.AutoScaleMode=AutoScaleMode.Dpi;
  form.StartPosition=FormStartPosition.CenterScreen;
 }

 public static Label Label(string text,int size=10){
  return new Label{Text=text,ForeColor=Text,AutoSize=true,Font=new Font("Segoe UI",size,size>=16?FontStyle.Bold:FontStyle.Regular),Margin=new Padding(4,8,8,6)};
 }

 public static Label Caption(string text,Color? color=null){
  var label=Label(text);
  label.ForeColor=color??Muted;
  return label;
 }

 public static Button Button(string text,Action action,bool primary=false){
  var button=new Button{
   Text=text,AutoSize=true,MinimumSize=new Size(100,40),FlatStyle=FlatStyle.Flat,
   BackColor=primary?Accent:Raised,ForeColor=primary?Background:Text,
   Margin=new Padding(4,6,6,4),Padding=new Padding(10,0,10,0),
   Font=new Font("Segoe UI Semibold",10),Cursor=Cursors.Hand,UseVisualStyleBackColor=false
  };
  button.FlatAppearance.BorderSize=1;
  button.FlatAppearance.BorderColor=primary?Accent:Border;
  button.FlatAppearance.MouseOverBackColor=primary?Color.FromArgb(157,246,218):Border;
  button.Click+=(sender,args)=>Safe(action);
  return button;
 }

 public static FlowLayoutPanel FormBody(Form form, Padding padding){
  var body=Flow(true);
  body.AutoSize=false;
  body.AutoScroll=true;
  body.Dock=DockStyle.Fill;
  body.Padding=padding;
  body.BackColor=Surface;
  form.Controls.Add(body);
  var rail=new Panel{Dock=DockStyle.Top,Height=4,BackColor=Accent};
  form.Controls.Add(rail);
  rail.BringToFront();
  return body;
 }

 public static Control BrandPanel(){
  var panel=new Panel{Dock=DockStyle.Fill,BackColor=Background};
  panel.Paint+=(sender,args)=>{
   var bounds=panel.ClientRectangle;
   if(bounds.Width<=0||bounds.Height<=0)return;
   var graphics=args.Graphics;
   graphics.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
   using(var fill=new System.Drawing.Drawing2D.LinearGradientBrush(bounds,Color.FromArgb(21,44,64),Color.FromArgb(13,25,42),55f))
    graphics.FillRectangle(fill,bounds);

   float left=bounds.Width*.12f, top=bounds.Height*.49f, width=bounds.Width*.76f, height=bounds.Height*.34f;
   using(var grid=new Pen(Color.FromArgb(45,Accent),1)){
    for(int row=0;row<=4;row++)
     graphics.DrawLine(grid,left,top+height*row/4,left+width,top+height*row/4);
   }
   float[] values={.79f,.68f,.73f,.44f,.50f,.21f,.27f,.08f};
   var points=new PointF[values.Length];
   for(int i=0;i<values.Length;i++)points[i]=new PointF(left+width*i/(values.Length-1),top+height*values[i]);
   var area=new PointF[points.Length+2];
   points.CopyTo(area,0);
   area[area.Length-2]=new PointF(left+width,top+height);
   area[area.Length-1]=new PointF(left,top+height);
   using(var tint=new SolidBrush(Color.FromArgb(36,Accent)))graphics.FillPolygon(tint,area);
   using(var stroke=new Pen(Accent,3))graphics.DrawLines(stroke,points);
   using(var dot=new SolidBrush(Accent))graphics.FillEllipse(dot,points[7].X-4,points[7].Y-4,8,8);
  };
  var copy=Flow(true);
  copy.Location=new Point(25,40);
  copy.BackColor=Color.Transparent;
  copy.Controls.Add(Caption("PERSONAL FINANCE",Accent));
  copy.Controls.Add(Label("IntelliBudget",25));
  panel.Controls.Add(copy);
  return panel;
 }
 public static void Safe(Action action){try{action();}catch(SqlException e){
  var message=e.Number==2601||e.Number==2627?"That username or category already exists.":e.Number==547?"That change conflicts with a database rule or a linked record.":e.Number==1205?"Another change ran at the same time. Please retry.":"SQL Server could not complete the request. Check the connection in App.config and the six tables. SQL error: "+e.Number;
  MessageBox.Show(message,"IntelliBudget",MessageBoxButtons.OK,MessageBoxIcon.Warning);
 }catch(InvalidOperationException e){MessageBox.Show(e.Message,"IntelliBudget",MessageBoxButtons.OK,MessageBoxIcon.Information);}
 catch(System.IO.IOException e){MessageBox.Show(e.Message,"Export failed");}
 catch(UnauthorizedAccessException e){MessageBox.Show(e.Message,"Export failed");}}
 public static FlowLayoutPanel Flow(bool vertical=false)=>new FlowLayoutPanel{AutoSize=true,WrapContents=false,FlowDirection=vertical?FlowDirection.TopDown:FlowDirection.LeftToRight,Padding=new Padding(0,3,0,3)};
 public static TextBox Input(int max,bool secret=false)=>new TextBox{Width=330,MaxLength=max,UseSystemPasswordChar=secret,BackColor=Raised,ForeColor=Text,BorderStyle=BorderStyle.FixedSingle,Margin=new Padding(4,2,8,8)};
 public static string Money(decimal value)=>"LKR "+value.ToString("N2");
 public static void Require(bool ok,string message){if(!ok)throw new InvalidOperationException(message);}
}
public sealed class LoginForm:Form {
 readonly TextBox username=UI.Input(50), password=UI.Input(128,true);
 public int UserId{get;private set;} public string Username{get;private set;}
 public LoginForm(){
  UI.Style(this); Text="IntelliBudget | Sign in"; ClientSize=new Size(810,425);FormBorderStyle=FormBorderStyle.FixedDialog;MaximizeBox=false;
  var shell=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=1,Margin=Padding.Empty};
  shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,44));
  shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,56));
  Controls.Add(shell);
  var brand=UI.BrandPanel();
  brand.Margin=Padding.Empty;
  shell.Controls.Add(brand,0,0);
  var body=UI.Flow(true);
  body.AutoSize=false;
  body.Dock=DockStyle.Fill;
  body.BackColor=UI.Surface;
  body.Padding=new Padding(30,36,14,16);
  body.Margin=Padding.Empty;
  shell.Controls.Add(body,1,0);
  body.Controls.Add(UI.Label("Welcome back",25));
  body.Controls.Add(UI.Caption("Sign in to manage your budget."));
  username.AccessibleName="Username";
  password.AccessibleName="Password";
  body.Controls.Add(UI.Label("Username"));body.Controls.Add(username);body.Controls.Add(UI.Label("Password"));body.Controls.Add(password);
  var actions=UI.Flow();body.Controls.Add(actions);AcceptButton=UI.Button("Sign in",SignIn,true);actions.Controls.Add((Button)AcceptButton);
  actions.Controls.Add(UI.Button("Create account",()=>{using(var f=new RegistrationForm())if(f.ShowDialog(this)==DialogResult.OK){username.Text=f.Username;password.Focus();}}));
 }
 void SignIn(){
  UI.Require(username.Text.Trim().Length>0&&password.Text.Length>0,"Enter your username and password.");
  DataTable rows;using(var db=new Store())rows=db.Read("SELECT UserID,Username,PasswordHash FROM dbo.Users WHERE Username=@p0",username.Text.Trim());
  var row=rows.Rows.Count==1?rows.Rows[0]:null;
  UI.Require(row!=null&&Passwords.Verify(password.Text,(string)row["PasswordHash"]),"Username or password is incorrect.");
  UserId=(int)row["UserID"];Username=(string)row["Username"];DialogResult=DialogResult.OK;
 }
}
public sealed class RegistrationForm:Form {
 public string Username{get;private set;}
 public RegistrationForm(){
  UI.Style(this);Text="IntelliBudget | Create account";ClientSize=new Size(454,449);FormBorderStyle=FormBorderStyle.FixedDialog;MaximizeBox=false;
  var body=UI.FormBody(this,new Padding(34,25,20,20));
  body.Controls.Add(UI.Label("Create your account",23));body.Controls.Add(UI.Caption("Use at least 12 characters for your password."));
  var u=UI.Input(50);var p=UI.Input(128,true);var c=UI.Input(128,true);
  body.Controls.Add(UI.Label("Username"));body.Controls.Add(u);body.Controls.Add(UI.Label("Password"));body.Controls.Add(p);body.Controls.Add(UI.Label("Confirm password"));body.Controls.Add(c);
  var actions=UI.Flow();body.Controls.Add(actions);
  var save=UI.Button("Create account",()=>{
   var name=u.Text.Trim();UI.Require(name.Length>=3,"Use at least 3 characters for your username.");
   UI.Require(p.Text.Length>=12&&p.Text==c.Text,"Use at least 12 characters and make sure the passwords match.");
   using(var db=new Store())db.Run("INSERT dbo.Users(Username,PasswordHash) VALUES(@p0,@p1)",name,Passwords.Hash(p.Text));
   Username=name;DialogResult=DialogResult.OK;
  },true);actions.Controls.Add(save);var cancel=UI.Button("Cancel",()=>Close());actions.Controls.Add(cancel);CancelButton=cancel;AcceptButton=save;
 }
}
}
