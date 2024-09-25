/*
****************************************************************************
*  Copyright (c) 2024,  Skyline Communications NV  All Rights Reserved.    *
****************************************************************************

By using this script, you expressly agree with the usage terms and
conditions set out below.
This script and all related materials are protected by copyrights and
other intellectual property rights that exclusively belong
to Skyline Communications.

A user license granted for this script is strictly for personal use only.
This script may not be used in any way by anyone without the prior
written consent of Skyline Communications. Any sublicensing of this
script is forbidden.

Any modifications to this script by the user are only allowed for
personal use and within the intended purpose of the script,
and will remain the sole responsibility of the user.
Skyline Communications will not be responsible for any damages or
malfunctions whatsoever of the script resulting from a modification
or adaptation by the user.

The content of this script is confidential information.
The user hereby agrees to keep this confidential information strictly
secret and confidential and not to disclose or reveal it, in whole
or in part, directly or indirectly to any person, entity, organization
or administration without the prior written consent of
Skyline Communications.

Any inquiries can be addressed to:

	Skyline Communications NV
	Ambachtenstraat 33
	B-8870 Izegem
	Belgium
	Tel.	: +32 51 31 35 69
	Fax.	: +32 51 31 01 29
	E-mail	: info@skyline.be
	Web		: www.skyline.be
	Contact	: Ben Vandenberghe

****************************************************************************
Revision History:

DATE		VERSION		AUTHOR			COMMENTS

02/09/2024	1.0.0.1		DPR, Skyline	Initial version
****************************************************************************
*/

namespace RetrieveAvailableNodes_1
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Analytics.GenericInterface;
	using Skyline.DataMiner.Net.Messages;

	[GQIMetaData(Name = "Collect_All_Nodes")]

	public class CollectNodes : IGQIDataSource, IGQIOnInit
	{
		private GQIDMS _dms;

		public GQIColumn[] GetColumns()
		{
			return new GQIColumn[]
			{
			new GQIIntColumn("Index"),
			new GQIStringColumn("Name"),
			new GQIStringColumn("State"),
			new GQIStringColumn("Protocol"),
			new GQIStringColumn("Alarm State"),
			};
		}

		public GQIPage GetNextPage(GetNextPageInputArgs args)
		{
			return GetData();
		}

		public OnInitOutputArgs OnInit(OnInitInputArgs args)
		{
			_dms = args.DMS;
			return default;
		}

		private GQIPage GetData()
		{
			var elements = GetElements().ToList();

			if (elements.Count == 0)
			{
				return new GQIPage(new List<GQIRow>().ToArray()) { HasNextPage = false };
			}

			var rows = new List<GQIRow>();

			foreach (var element in elements)
			{
				var alarmState = GetElementAlarmState(element.ElementID, element.DataMinerID);
				var cells = new List<GQICell>
				{
					new GQICell { Value = element.ElementID},
					new GQICell { Value = element.Name},
					new GQICell { Value = element.State.ToString() },
					new GQICell { Value = element.Protocol },
					new GQICell { Value = alarmState},
				};

				var rowData = new GQIRow(cells.ToArray());
				rows.Add(rowData);
			}

			return new GQIPage(rows.ToArray()) { HasNextPage = false };
		}

		private List<LiteElementInfoEvent> GetElements()
		{
			var huaweiRequest = new GetLiteElementInfo(includeStopped: false)
			{
				ProtocolName = "Huawei Manager",
				ProtocolVersion = "Production",
			};

			var juniperRequest = new GetLiteElementInfo(includeStopped: false)
			{
				ProtocolName = "Juniper Networks Manager",
				ProtocolVersion = "Production",
			};

			var ciscoRequest = new GetLiteElementInfo(includeStopped: false)
			{
				ProtocolName = "CISCO ASR Manager",
				ProtocolVersion = "Production",
			};

			try
			{
				var huaweiElements = _dms.SendMessages(huaweiRequest).OfType<LiteElementInfoEvent>().ToList();
				var juniperElements = _dms.SendMessages(juniperRequest).OfType<LiteElementInfoEvent>().ToList();
				var ciscoElements = _dms.SendMessages(ciscoRequest).OfType<LiteElementInfoEvent>().ToList();
				juniperElements.AddRange(ciscoElements);
				huaweiElements.AddRange(juniperElements);

				return huaweiElements;
			}
			catch (Exception)
			{
				return null;
			}
		}

		private string GetElementAlarmState (Int32 elementID, Int32 dmaID)
		{
			int alarmStateParam = 65008;
			var request = new GetParameterMessage()
			{
				DataMinerID = dmaID,
				ElId = elementID,
				ParameterId = alarmStateParam,
			};

			try
			{
				var alarmStateMessage = _dms.SendMessages(request);
				var alarmState = alarmStateMessage.OfType<GetParameterResponseMessage>().ToList();

				return alarmState[0].DisplayValue;
			}
			catch (Exception)
			{
				return null;
			}
		}

		public class Element
		{
			public string Index { get; set; }

			public string Name { get; set; }

			public string State { get; set; }

			public double Protocol { get; set; }

			public string AlarmState { get; set; }
		}
	}
}