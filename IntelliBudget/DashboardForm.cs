using System;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.IO;

namespace IntelliBudget {
public sealed partial class DashboardForm:Form {
 readonly int user;
 readonly string username;
 readonly Panel content=new Panel{Dock=DockStyle.Fill,Padding=new Padding(28,22,28,20)};
 readonly Panel pageHost=new Panel{Dock=DockStyle.Fill,Margin=Padding.Empty};
 readonly Label heading=UI.Label("Dashboard",27);
 readonly Label monthLabel=UI.Caption("Budget month");
 readonly FlowLayoutPanel toolbar=UI.Flow();
 readonly DateTimePicker period=new DateTimePicker{Format=DateTimePickerFormat.Custom,CustomFormat="MMMM yyyy",ShowUpDown=true,Width=175,MinDate=new DateTime(1900,1,1),MaxDate=new DateTime(9998,12,31)};
 readonly string preferences;
 readonly Button[] navigation=new Button[8];
 bool reserve=true;
 int screen;
 public bool SignedOut{get;private set;}
 DateTime Month=>new DateTime(period.Value.Year,period.Value.Month,1);
 static readonly string[] Names={"Dashboard","Planned income","Fixed expenses","Savings goals","Budget planner","Transactions","Reports","Settings"};
 public DashboardForm():this(0,"Preview"){}
 internal DashboardForm(int id,string name){
  user=id;username=name; preferences=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"IntelliBudget","savings-"+id+".txt");
  try{if(File.Exists(preferences))reserve=File.ReadAllText(preferences)!="track";}catch(IOException){}catch(UnauthorizedAccessException){}
  UI.Style(this);
  Text="IntelliBudget";
  ClientSize=new Size(1180,740);
  MinimumSize=new Size(930,650);

  var shell=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=1};
  shell.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,218));
  shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
  Controls.Add(shell);
  var sidebar=new Panel{Dock=DockStyle.Fill,BackColor=UI.Surface,Margin=Padding.Empty};
  shell.Controls.Add(sidebar,0,0);
  content.Margin=Padding.Empty;
  shell.Controls.Add(content,1,0);
  var layout=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=3,Margin=Padding.Empty};
  layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
  layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
  layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
  layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));
  content.Controls.Add(layout);
  heading.Margin=new Padding(4,3,4,12);
  layout.Controls.Add(heading,0,0);
  toolbar.BackColor=UI.Surface;
  toolbar.Padding=new Padding(10,2,6,3);
  toolbar.Margin=new Padding(4,0,4,18);
  monthLabel.Margin=new Padding(4,15,8,0);
  toolbar.Controls.Add(monthLabel);
  period.Margin=new Padding(4,12,8,0);
  toolbar.Controls.Add(period);
  toolbar.Controls.Add(UI.Button("Refresh",RefreshPage));
  layout.Controls.Add(toolbar,0,1);
  layout.Controls.Add(pageHost,0,2);

  var side=UI.Flow(true);
  side.AutoSize=false;
  side.AutoScroll=true;
  side.Dock=DockStyle.Fill;
  side.Padding=new Padding(14,18,12,14);
  sidebar.Controls.Add(side);
  var brand=UI.Label("IntelliBudget",20);
  brand.ForeColor=UI.Accent;
  brand.Margin=new Padding(7,8,0,0);
  side.Controls.Add(brand);
  var caption=UI.Caption("Personal finance");
  caption.Margin=new Padding(8,3,0,22);
  side.Controls.Add(caption);

  string[] icons={"◈","↗","▤","◎","▦","⇄","▥","⚙"};
  for(int i=0;i<Names.Length;i++){
   int index=i;
   var button=UI.Button(icons[i]+"   "+Names[i],()=>{screen=index;RefreshPage();});
   button.AutoSize=false;
   button.Size=new Size(182,40);
   button.Margin=new Padding(2,0,2,4);
   button.TextAlign=ContentAlignment.MiddleLeft;
   button.FlatAppearance.BorderSize=0;
   navigation[i]=button;
   side.Controls.Add(button);
  }

  var footer=UI.Flow(true);
  footer.AutoSize=false;
  footer.Height=95;
  footer.BackColor=UI.Surface;
  footer.Dock=DockStyle.Bottom;
  footer.Padding=new Padding(14,8,10,15);
  footer.Controls.Add(new Label{
   Text=username,ForeColor=UI.Muted,AutoEllipsis=true,Size=new Size(182,24),Margin=new Padding(6,3,0,0)
  });
  var signOut=UI.Button("Sign out",()=>{SignedOut=true;Close();});
  signOut.Margin=new Padding(2,3,2,0);
  footer.Controls.Add(signOut);
  sidebar.Controls.Add(footer);
  footer.BringToFront();
  UpdateNavigation();
  period.ValueChanged+=(s,e)=>UI.Safe(RefreshPage);
  Shown+=(s,e)=>{if(user>0)UI.Safe(RefreshPage);};
 }
 void UpdateNavigation(){
  for(int i=0;i<navigation.Length;i++){
   var button=navigation[i];
   button.BackColor=i==screen?UI.Raised:UI.Surface;
   button.ForeColor=i==screen?UI.Accent:UI.Muted;
   button.AccessibleDescription=i==screen?"Current page":string.Empty;
  }
 }

 void RefreshPage(){
  UpdateNavigation();
  heading.Text=Names[screen];
  toolbar.Visible=screen!=7;
  monthLabel.Text=screen==5?"Transaction month":"Budget month";
  if(user<=0)return;
  foreach(Control old in pageHost.Controls.Cast<Control>().ToArray())old.Dispose();
  var page=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,AutoScroll=true};
  pageHost.Controls.Add(page);
  page.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
  if(screen==7){Settings(page);return;}
  using(var db=new Store()){
   if(screen==5){Transactions(page,db);return;}
   var budget=db.Budget(user,Month);var plan=Plan.Load(db,budget,reserve);
   switch(screen){
    case 0:Overview(page,db,budget,plan);break;
    case 1:Income(page,budget);break;
    case 2:Entries(page,db,budget,"FixedExpenses","ExpenseID","Category","Amount",plan);break;
    case 3:Entries(page,db,budget,"SavingsGoals","GoalID","GoalName","GoalValue",plan);break;
    case 4:Entries(page,db,budget,"BudgetAllocations","AllocationID","Category","AllocatedAmount",plan);break;
    case 6:Reports(page,db,budget,plan);break;
   }
  }
 }
 void Settings(TableLayoutPanel page){
  page.Controls.Add(UI.Label("Signed in as "+username,16));
  page.Controls.Add(UI.Label("Currency: LKR  |  Theme: Dark"));
  var mode=new CheckBox{Text="Reserve savings before allocating spending",Checked=reserve,AutoSize=true,ForeColor=UI.Text};
  page.Controls.Add(mode);
  page.Controls.Add(UI.Label("Clear this to track savings goals without subtracting them from spending."));
  page.Controls.Add(UI.Label("This preference applies to all months on this Windows profile."));
  page.Controls.Add(UI.Button("Save preference",()=>{
   Directory.CreateDirectory(Path.GetDirectoryName(preferences));File.WriteAllText(preferences,mode.Checked?"reserve":"track");reserve=mode.Checked;
   MessageBox.Show(this,"Savings preference saved.","IntelliBudget");
  },true));
 }
 void Income(TableLayoutPanel page,DataRow budget){
  page.Controls.Add(UI.Caption("Enter the income you expect for this month. Record payments received in Transactions."));
  var income=Amount(budget==null?0:(decimal)budget["PlannedIncome"]);
  page.Controls.Add(UI.Label("Planned income (LKR)"));page.Controls.Add(income);
  page.Controls.Add(UI.Button("Save income",()=>{
   UI.Require(income.Value>0,"Enter income greater than zero.");
   ChangePlan((db,row)=>{
    if(row==null)db.Run("INSERT dbo.MonthlyBudgets(UserID,BudgetMonth,PlannedIncome) VALUES(@p0,@p1,@p2)",user,Month,income.Value);
    else db.Run("UPDATE dbo.MonthlyBudgets SET PlannedIncome=@p0 WHERE UserID=@p1 AND BudgetMonth=@p2",income.Value,user,Month);
   });
  },true));
  if(budget!=null)page.Controls.Add(UI.Button("Delete monthly plan",()=>{
   if(MessageBox.Show(this,"Delete the income, fixed expenses, goals and allocations for this month? Actual transactions will remain.","Delete monthly plan",MessageBoxButtons.YesNo)!=DialogResult.Yes)return;
   ChangePlan((db,row)=>{
    if(row==null)return;int id=(int)row["BudgetID"];
    foreach(var table in new[]{"FixedExpenses","SavingsGoals","BudgetAllocations"})db.Run("DELETE dbo.["+table+"] WHERE BudgetID=@p0",id);
    db.Run("DELETE dbo.MonthlyBudgets WHERE BudgetID=@p0 AND UserID=@p1",id,user);
   });
  }));
 }
 void ChangePlan(Action<Store,DataRow> mutation){
  using(var db=new Store())db.Atomic(()=>{
   var row=db.Budget(user,Month,true);var before=Plan.Load(db,row,reserve);
   mutation(db,row);var after=Plan.Load(db,db.Budget(user,Month),reserve);
   UI.Require(after.Remaining>=0||after.Remaining>before.Remaining,"This change exceeds the available monthly income. Reduce expenses, reserved savings or allocations first.");
  });
  RefreshPage();
 }
 static NumericUpDown Amount(decimal value=0)=>new NumericUpDown{DecimalPlaces=2,ThousandsSeparator=true,Width=210,Maximum=9999999999999999.99m,Value=value,BackColor=UI.Surface,ForeColor=UI.Text};
 static DataGridView Grid(DataTable table,string id=null){
  var g=new DataGridView{Dock=DockStyle.Fill,Height=280,ReadOnly=true,AllowUserToAddRows=false,AllowUserToDeleteRows=false,RowHeadersVisible=false,SelectionMode=DataGridViewSelectionMode.FullRowSelect,MultiSelect=false,AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill,BackgroundColor=UI.Surface,BorderStyle=BorderStyle.None,EnableHeadersVisualStyles=false,DataSource=table};
  g.RowTemplate.Height=38;
  g.ColumnHeadersHeight=42;
  g.ColumnHeadersHeightSizeMode=DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
  g.CellBorderStyle=DataGridViewCellBorderStyle.SingleHorizontal;
  g.ColumnHeadersBorderStyle=DataGridViewHeaderBorderStyle.None;
  g.GridColor=UI.Border;
  g.Margin=new Padding(4,6,4,12);
  g.DefaultCellStyle.BackColor=UI.Surface;
  g.DefaultCellStyle.ForeColor=UI.Text;
  g.DefaultCellStyle.SelectionBackColor=UI.Border;
  g.DefaultCellStyle.SelectionForeColor=UI.Text;
  g.DefaultCellStyle.Padding=new Padding(8,0,8,0);
  g.AlternatingRowsDefaultCellStyle.BackColor=UI.Raised;
  g.ColumnHeadersDefaultCellStyle.BackColor=UI.Raised;
  g.ColumnHeadersDefaultCellStyle.ForeColor=UI.Muted;
  g.ColumnHeadersDefaultCellStyle.SelectionBackColor=UI.Raised;
  g.ColumnHeadersDefaultCellStyle.Padding=new Padding(8,0,8,0);
  g.BindingContext=new BindingContext();
  foreach(DataGridViewColumn c in g.Columns){
   if(c.Name==id||c.Name=="BudgetID")c.Visible=false;
   if(c.ValueType==typeof(decimal)){
    c.DefaultCellStyle.Format="N2";
    c.DefaultCellStyle.Alignment=DataGridViewContentAlignment.MiddleRight;
   }
   if(c.ValueType==typeof(DateTime))c.DefaultCellStyle.Format="yyyy-MM-dd";
   c.HeaderText=System.Text.RegularExpressions.Regex.Replace(c.Name,"(?<=[a-z])(?=[A-Z])"," ");
  }return g;
 }
 static DataRow Selected(DataGridView grid){var row=grid.CurrentRow?.DataBoundItem as DataRowView;UI.Require(row!=null,"Select a row first.");return row.Row;}
 static bool Confirm()=>MessageBox.Show("Delete the selected record?","IntelliBudget",MessageBoxButtons.YesNo,MessageBoxIcon.Question)==DialogResult.Yes;
 void Entries(TableLayoutPanel page,Store db,DataRow row,string table,string id,string name,string value,Plan plan){
  if(row==null){page.Controls.Add(UI.Label("Save planned income for this month first."));page.Controls.Add(UI.Button("Go to planned income",()=>{screen=1;RefreshPage();},true));return;}
  bool goal=table=="SavingsGoals";int budget=(int)row["BudgetID"];
  if(table=="BudgetAllocations")page.Controls.Add(UI.Label("Available: "+UI.Money(plan.Available)+"    Allocated: "+UI.Money(plan.Allocated)+"    Remaining: "+UI.Money(plan.Remaining),12));
  if(goal)page.Controls.Add(UI.Label(reserve?"Savings are reserved from planned income.":"Goals are tracked separately from spending."));
  var list=db.Entries(table,budget);
  if(goal){list.Columns.Add("SavingsLKR",typeof(decimal));foreach(DataRow r in list.Rows)r["SavingsLKR"]=Plan.Goal(plan.Income,(string)r["GoalType"],(decimal)r[value]);}
  var grid=Grid(list,id);page.Controls.Add(grid);
  var actions=UI.Flow();page.Controls.Add(actions);
  Action<DataRow> edit=r=>{
   using(var f=new EditorForm(goal?"Savings goal":Names[screen],r,goal,false,name,value,Month)){
    if(f.ShowDialog(this)!=DialogResult.OK)return;
    ChangePlan((store,current)=>{
     UI.Require(current!=null&&(int)current["BudgetID"]==budget,"The monthly plan changed. Refresh and try again.");
     string columns=goal?name+",GoalType,"+value:name+","+value;
     string values=goal?"@p1,@p3,@p2":"@p1,@p2";
     string set=name+"=@p1,"+value+"=@p2"+(goal?",GoalType=@p3":"");
     if(r==null)store.Run("INSERT dbo.["+table+"](BudgetID,"+columns+") VALUES(@p0,"+values+")",budget,f.EntryName,f.EntryAmount,f.Kind);
     else UI.Require(store.Run("UPDATE dbo.["+table+"] SET "+set+" WHERE BudgetID=@p0 AND "+id+"=@p4",budget,f.EntryName,f.EntryAmount,f.Kind,r[id])==1,"That record no longer exists.");
    });
   }
  };
  actions.Controls.Add(UI.Button("Add",()=>edit(null),true));actions.Controls.Add(UI.Button("Edit",()=>edit(Selected(grid))));
  actions.Controls.Add(UI.Button("Delete",()=>{var r=Selected(grid);if(Confirm())ChangePlan((s,b)=>{
   UI.Require(b!=null&&(int)b["BudgetID"]==budget,"The monthly plan changed. Refresh and try again.");
   s.Run("DELETE dbo.["+table+"] WHERE BudgetID=@p0 AND "+id+"=@p1",budget,r[id]);
  });}));
 }
 void Transactions(TableLayoutPanel page,Store db){
  var month=Month;
  var rows=db.Transactions(user,month,month.AddMonths(1));
  page.Controls.Add(UI.Caption("Use the same categories as your plan to compare spending."));
  var summary=rows.Rows.Count==0?"No transactions for "+month.ToString("MMMM yyyy")+".":rows.Rows.Count+" "+(rows.Rows.Count==1?"transaction":"transactions")+" for "+month.ToString("MMMM yyyy")+".";
  page.Controls.Add(UI.Caption(summary));
  var grid=Grid(rows,"TransactionID");page.Controls.Add(grid);
  var actions=UI.Flow();page.Controls.Add(actions);
  Action<DataRow> edit=r=>{
   using(var f=new EditorForm("Transaction",r,false,true,"Category","Amount",Month)){
    if(f.ShowDialog(this)!=DialogResult.OK)return;
    using(var store=new Store()){
     if(r==null)store.Run("INSERT dbo.Transactions(UserID,Category,Amount,TransactionType,TransactionDate,Notes) VALUES(@p0,@p1,@p2,@p3,@p4,@p5)",user,f.EntryName,f.EntryAmount,f.Kind,f.EntryDate,f.Notes);
     else UI.Require(store.Run("UPDATE dbo.Transactions SET Category=@p1,Amount=@p2,TransactionType=@p3,TransactionDate=@p4,Notes=@p5 WHERE UserID=@p0 AND TransactionID=@p6",user,f.EntryName,f.EntryAmount,f.Kind,f.EntryDate,f.Notes,r["TransactionID"])==1,"That transaction no longer exists.");
    }
    if(f.EntryDate>=period.MinDate&&f.EntryDate<=period.MaxDate&&
       (f.EntryDate.Year!=month.Year||f.EntryDate.Month!=month.Month))period.Value=f.EntryDate;
    else RefreshPage();
   }
  };
  actions.Controls.Add(UI.Button("Add transaction",()=>edit(null),true));actions.Controls.Add(UI.Button("Edit",()=>edit(Selected(grid))));
  actions.Controls.Add(UI.Button("Delete",()=>{var r=Selected(grid);if(!Confirm())return;using(var s=new Store())s.Run("DELETE dbo.Transactions WHERE UserID=@p0 AND TransactionID=@p1",user,r["TransactionID"]);RefreshPage();}));
 }
}
}
