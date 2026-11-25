using System;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using System.Linq;
using System.Security.Cryptography;

namespace IntelliBudget {
internal sealed class Store : IDisposable {
 readonly SqlConnection connection = new SqlConnection(ConfigurationManager.ConnectionStrings["Budget"].ConnectionString);
 SqlTransaction transaction;
 public Store(){ try { connection.Open(); } catch { connection.Dispose(); throw; } }
 public void Dispose(){ transaction?.Dispose(); connection.Dispose(); }
 public void Atomic(Action action){
  using(var active=connection.BeginTransaction(IsolationLevel.Serializable)){
   transaction=active;
   try { action(); active.Commit(); } finally { transaction=null; }
  }
 }
 SqlCommand Cmd(string sql, object[] args){
  var cmd=new SqlCommand(sql,connection,transaction);
  for(int i=0;i<args.Length;i++)cmd.Parameters.AddWithValue("@p"+i,args[i]??DBNull.Value);
  return cmd;
 }
 public int Run(string sql, params object[] args){using(var c=Cmd(sql,args))return c.ExecuteNonQuery();}
 public DataTable Read(string sql,params object[] args){
  using(var c=Cmd(sql,args))using(var r=c.ExecuteReader()){var t=new DataTable();t.Load(r);return t;}
 }
 public DataRow Budget(int user,DateTime month,bool edit=false){
  return Read("SELECT BudgetID,PlannedIncome FROM dbo.MonthlyBudgets "+(edit?"WITH (UPDLOCK,HOLDLOCK) ":"")+"WHERE UserID=@p0 AND BudgetMonth=@p1",user,month).Rows.Cast<DataRow>().SingleOrDefault();
 }
 public DataTable Entries(string table,int budget){return Read("SELECT * FROM dbo.["+table+"] WHERE BudgetID=@p0",budget);}
 public DataTable Transactions(int user,DateTime start,DateTime end){return Read("SELECT TransactionID,TransactionDate,TransactionType,Category,Amount,Notes FROM dbo.Transactions WHERE UserID=@p0 AND TransactionDate>=@p1 AND TransactionDate<@p2 ORDER BY TransactionDate DESC,TransactionID DESC",user,start,end);}
}
internal static class Passwords {
 const int Rounds=600000;
 static byte[] Derive(string password,byte[] salt){using(var k=new Rfc2898DeriveBytes(password,salt,Rounds,HashAlgorithmName.SHA256))return k.GetBytes(32);}
 public static string Hash(string password){var salt=new byte[16];using(var rng=RandomNumberGenerator.Create())rng.GetBytes(salt);return "pbkdf2-sha256$"+Rounds+"$"+Convert.ToBase64String(salt)+"$"+Convert.ToBase64String(Derive(password,salt));}
 public static bool Verify(string password,string hash){
  var fields=hash.Split('$'); if(fields.Length!=4||fields[0]!="pbkdf2-sha256"||fields[1]!=Rounds.ToString())return false;
  try{
   var salt=Convert.FromBase64String(fields[2]);var expected=Convert.FromBase64String(fields[3]);if(salt.Length!=16||expected.Length!=32)return false;
   var actual=Derive(password,salt);int diff=0;for(int i=0;i<32;i++)diff|=actual[i]^expected[i];return diff==0;
  }catch(FormatException){return false;}
 }
}
internal sealed class Plan {
 public decimal Income,Fixed,Savings,Allocated;
 public decimal Available=>Income-Fixed-Savings;
 public decimal Remaining=>Available-Allocated;
 public static Plan Load(Store db, DataRow budget,bool reserve){
  var p=new Plan();if(budget==null)return p;
  int id=(int)budget["BudgetID"];p.Income=(decimal)budget["PlannedIncome"];
  p.Fixed=db.Entries("FixedExpenses",id).Rows.Cast<DataRow>().Sum(r=>(decimal)r["Amount"]);
  if(reserve)p.Savings=db.Entries("SavingsGoals",id).Rows.Cast<DataRow>().Sum(r=>Goal(p.Income,(string)r["GoalType"],(decimal)r["GoalValue"]));
  p.Allocated=db.Entries("BudgetAllocations",id).Rows.Cast<DataRow>().Sum(r=>(decimal)r["AllocatedAmount"]);return p;
 }
 public static decimal Goal(decimal income,string type,decimal value)=>type=="Percentage"?Math.Round(income*value/100m,2,MidpointRounding.AwayFromZero):value;
}
}
