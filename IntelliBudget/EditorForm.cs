using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
namespace IntelliBudget {
public sealed class EditorForm:Form{
 public string EntryName{get;private set;}public decimal EntryAmount{get;private set;}public string Kind{get;private set;}public DateTime EntryDate{get;private set;}public string Notes{get;private set;}
 public EditorForm():this("Transaction",null,false,true,"Category","Amount",DateTime.Today){}
 internal EditorForm(string title,DataRow row,bool goal,bool transaction,string name,string value,DateTime month){
  UI.Style(this);Text=title;ClientSize=new Size(464,transaction?532:400);FormBorderStyle=FormBorderStyle.FixedDialog;MaximizeBox=false;StartPosition=FormStartPosition.CenterParent;
  var body=UI.FormBody(this,new Padding(30,22,18,16));
  body.Controls.Add(UI.Label((row==null?"Add ":"Edit ")+title.ToLowerInvariant(),20));
  var label=UI.Label("Amount (LKR)");var input=UI.Input(100);var amount=new NumericUpDown{Width=330,DecimalPlaces=2,ThousandsSeparator=true,Maximum=9999999999999999.99m,BackColor=UI.Raised,ForeColor=UI.Text};
  var kind=new ComboBox{Width=330,DropDownStyle=ComboBoxStyle.DropDownList,FlatStyle=FlatStyle.Flat,BackColor=UI.Raised,ForeColor=UI.Text,AccessibleName="Type"};
  var date=new DateTimePicker{Width=330,Format=DateTimePickerFormat.Short,AccessibleName="Date"};
  var notes=UI.Input(500);
  body.Controls.Add(UI.Label(goal?"Goal name":"Category"));body.Controls.Add(input);
  if(goal||transaction){kind.Items.AddRange(goal?new[]{"Fixed","Percentage"}:new[]{"Expense","Income"});kind.SelectedIndex=0;body.Controls.Add(UI.Label("Type"));body.Controls.Add(kind);}
  if(goal)kind.SelectedIndexChanged+=(s,e)=>label.Text=kind.Text=="Percentage"?"Percentage of planned income":"Amount (LKR)";
  body.Controls.Add(label);body.Controls.Add(amount);
  if(transaction){date.Value=month.Year==DateTime.Today.Year&&month.Month==DateTime.Today.Month?DateTime.Today:month;body.Controls.Add(UI.Label("Date"));body.Controls.Add(date);body.Controls.Add(UI.Label("Notes (optional)"));body.Controls.Add(notes);}
  if(row!=null){
   input.Text=(string)row[name];amount.Value=(decimal)row[value];
   if(goal)kind.SelectedItem=(string)row["GoalType"];
   if(transaction){kind.SelectedItem=(string)row["TransactionType"];date.Value=(DateTime)row["TransactionDate"];notes.Text=Convert.ToString(row["Notes"]);}
  }
  var actions=UI.Flow();body.Controls.Add(actions);
  var save=UI.Button("Save",()=>{
   UI.Require(input.Text.Trim().Length>0,"Enter a name or category.");
   UI.Require(value=="AllocatedAmount"||amount.Value>0,"Enter an amount greater than zero.");
   UI.Require(!(goal||transaction)||kind.SelectedIndex>=0,"Choose a supported type.");
   UI.Require(!goal||kind.Text!="Percentage"||amount.Value<=100,"The percentage cannot exceed 100.");
   EntryName=input.Text.Trim();EntryAmount=amount.Value;Kind=kind.Text;EntryDate=date.Value.Date;Notes=string.IsNullOrWhiteSpace(notes.Text)?null:notes.Text.Trim();DialogResult=DialogResult.OK;
  },true);actions.Controls.Add(save);var cancel=UI.Button("Cancel",()=>Close());actions.Controls.Add(cancel);CancelButton=cancel;AcceptButton=save;
 }
}
}
