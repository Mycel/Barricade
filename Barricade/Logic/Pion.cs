﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool
//     Changes to this file will be lost if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------
namespace Logic
{
	using Process;
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;

	public class Pion
	{
	    public Pion(Speler speler)
	    {
	        Speler = speler;
	    }

	    public virtual IVeld IVeld
		{
			get;
			set;
		}

		public virtual Speler Speler
		{
			get; private set;
		}

		public virtual List<IVeld> MogelijkeZetten(int stappen = 1)
		{
			throw new System.NotImplementedException();
		}

		public virtual bool Verplaats(IVeld bestemming)
		{
			throw new System.NotImplementedException();
		}

	}
}

