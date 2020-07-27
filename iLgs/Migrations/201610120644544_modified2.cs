namespace iLgs.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class modified2 : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.AspNetUsers", "DtRegs", c => c.DateTime(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.AspNetUsers", "DtRegs");
        }
    }
}
