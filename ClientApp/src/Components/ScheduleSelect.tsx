import React, {useMemo, useState} from 'react';
import {observer} from "mobx-react-lite";
import {Cron} from "../Utilities/DataTypes/Cron";
import './ScheduleSelect.css';
import {TextField} from "@fluentui/react";
import {useTranslation} from "react-i18next";
import Select from "react-select";
import Creatable from "react-select/creatable";

interface ISchedule {
    cron: Cron;
    disabled?: boolean;
}

export const ScheduleSelect = observer(({cron, disabled}: ISchedule)=>{
    function whichSchedule(cron: Cron){
        switch(cron.cycle){
            case "hourly": return <ScheduleHourly cron={cron} disabled={disabled}/>;
            case "daily": return <ScheduleDaily cron={cron} disabled={disabled}/>;
            case "weekly": return <ScheduleWeekly cron={cron} disabled={disabled}/>;
            case "monthly": return <ScheduleMonthly cron={cron} disabled={disabled}/>;
            case "yearly": return <ScheduleYearly cron={cron} disabled={disabled}/>;
            case "custom": return <CronWriter cron={cron} disabled={disabled}/>;
        }
    }
    const schedule = whichSchedule(cron);

    return (<div className={'schedule'}>
        <CyclePicker cron={cron} disabled={disabled}/>
        {schedule}
    </div>)
});

const CyclePicker = observer(({cron, disabled}: {cron: Cron, disabled?: boolean})=>{
    const { t } = useTranslation();
    const options = [
        {value: "hourly", label: t("Hourly")},
        {value: "daily", label: t("Daily")},
        {value: "weekly", label: t("Weekly")},
        {value: "monthly", label: t("Monthly")},
        {value: "yearly", label: t("Yearly")},
        {value: "custom", label: t("Custom (Cron Format)")},
    ];

    return (<Select
        value={options.find(val=>cron.cycle.indexOf(val.value) > -1)}
        options={options}
        onChange={(item: any)=>cron.setCycle(item.value)}
        disabled={disabled}
    />)
});

const MinutesPicker = observer(({cron, disabled}: {cron: Cron, disabled?: boolean})=>{
    const options = [];

    for(let i = 0; i < 60; i+=5){
        options.push({value: i.toString(), label: `0${i}`.slice(-2)})
    }

    //To keep the list short, we only include increments of 5. If another number is set, we add it
    cron.minutes
        .filter(val=>(+val % 5) !== 0)
        .forEach(val=>{
            options.push({value: val, label: `0${val}`.slice(-2)})
        })

    options.sort((a,b)=>a.value.localeCompare(b.value, 'en', {numeric: true}));

    return (<Creatable
        value={options.find(val=>cron.minutes.indexOf(val.value) > -1)}
        options={options}
        onChange={(item: any)=>cron.setMinutes([item.value])}
        onCreateOption={(value)=>cron.setMinutes([value])}
        formatCreateLabel={value=>{
            if(+value >= 0 && +value < 60){
                return `Set minutes to ${value.slice(-2)}`
            } else {
                return "---"
            }
        }}
        disabled={disabled}
    />)
});

const HoursPicker = observer(({cron, disabled}: {cron: Cron, disabled?: boolean})=>{
    const options = [];

    for(let i = 0; i < 24; i+=1){
        options.push({value: i.toString(), label: `${i}`})
    }

    return (<Select
        value={options.find(val=>cron.hours.indexOf(val.value) > -1)}
        options={options}
        onChange={(item: any)=>cron.setHours([item.value])}
        disabled={disabled}
    />)
});

const DaysOfWeekPicker = observer(({cron, disabled}: {cron: Cron, disabled?: boolean})=>{
    const { t } = useTranslation();

    const options = [
        {value: "*", label: t("Every Day")},
        {value: "0", label: t("Sunday")},
        {value: "1", label: t("Monday")},
        {value: "2", label: t("Tuesday")},
        {value: "3", label: t("Wednesday")},
        {value: "4", label: t("Thursday")},
        {value: "5", label: t("Friday")},
        {value: "6", label: t("Saturday")},
    ];

    return (<div>
        <Select
            value={options.filter(val=>cron.daysOfWeek.indexOf(val.value) > -1)}
            options={options}
            isMulti={true}
            onChange={(selected: any)=>cron.setDaysOfWeek((selected || []).map((val: any)=>val.value))}
            disabled={disabled}
        />
    </div>)
});

const CronWriter = observer(({cron, disabled}: {cron: Cron, disabled?: boolean})=>{
    const { t } = useTranslation();
    const [textValue, setTextValue] = useState(cron.toString());
    const [isValidCron, setIsValidCron] = useState(true);

    const currentCron: Cron | undefined = useMemo(()=>{
        if(isValidCron){
            return new Cron(textValue);
        } else {
            return undefined;
        }
    }, [isValidCron, textValue])

    function setCron(){
        if(Cron.IsValid(textValue)){
            cron.setExpression(textValue)
        }
    }

    function changeCurrentCron(e: any, value?: string){
        setTextValue(value || "")
        if(value && Cron.IsValid(value)){
            setIsValidCron(true);
        } else {
            setIsValidCron(false);
        }
    }

    return (<div style={{marginTop: "10px"}}>
        <TextField
            value={textValue.toString()}
            onChange={changeCurrentCron}
            onBlur={setCron}
            disabled={disabled}
            iconProps={{iconName: isValidCron ? t("Accept") : t("Cancel"), color: isValidCron ? '#00FF00' : '#FF0000'}}
        />
        <div style={{display: "flex"}}>
            <div style={{flex: 1}}>{currentCron?.description() || "---"}</div>
            <a href={"https://en.wikipedia.org/wiki/Cron#CRON_expression"} target={"_blank"}  rel="noopener noreferrer">Cron Format</a>
        </div>
    </div>)
});

const MonthsPicker = observer(({cron, disabled}: {cron: Cron, disabled?: boolean})=>{
    const { t } = useTranslation();
    const options = [
        {value: "*", label: t("Every Month")},
        {value: "1", label: t("January")},
        {value: "2", label: t("February")},
        {value: "3", label: t("March")},
        {value: "4", label: t("April")},
        {value: "5", label: t("May")},
        {value: "6", label: t("June")},
        {value: "7", label: t("July")},
        {value: "8", label: t("August")},
        {value: "9", label: t("September")},
        {value: "10", label: t("October")},
        {value: "11", label: t("November")},
        {value: "12", label: t("December")},
    ];

    return (<div className={"monthPicker"}>
        <Select
            value={options.filter(val=>cron.months.indexOf(val.value) > -1)}
            options={options}
            isMulti={true}
            onChange={(selected: any)=>cron.setMonths((selected || []).map((val: any)=>val.value))}
            disabled={disabled}
        />
    </div>)
});

const DaysPicker = observer(({cron, disabled}: {cron: Cron, disabled?: boolean})=>{

    function nth(d: number){
        if (d > 3 && d < 21) return d + 'th';
        switch (d % 10) {
            case 1:  return d + "st";
            case 2:  return d + "nd";
            case 3:  return d + "rd";
            default: return d + "th";
        }
    }
    const options = [{value: "*", label: "every day"}];

    for(let i = 1; i <= 31; i += 1){
        options.push({value: i.toString(), label: nth(i)})
    }


    return (<div className={"dayPicker"}>
        <Select
            value={options.filter(val=>cron.days.indexOf(val.value) > -1)}
            options={options}
            isMulti={true}
            onChange={(selected: any)=>cron.setDaysOfMonth((selected || []).map((val: any)=>val.value))}
            title={cron.days.join(', ')}
            disabled={disabled}
        />
    </div>)
});

const ScheduleHourly = observer(({cron, disabled}: ISchedule)=>{
    const { t } = useTranslation();
    return (<React.Fragment>
        <div>{t("at")}</div>
        <MinutesPicker cron={cron} disabled={disabled}/>
        <div>{t("minutes after the hour")}</div>
        <div>{cron?.description() || "---"}</div>
    </React.Fragment>)
});

const ScheduleDaily = observer(({cron, disabled}: ISchedule)=>{
    const { t } = useTranslation();
    return (<React.Fragment>
        <div>{t("at")}</div>

        <HoursPicker cron={cron} disabled={disabled}/>
        <MinutesPicker cron={cron} disabled={disabled}/>
        <div>{cron?.description() || "---"}</div>
    </React.Fragment>)
});

const ScheduleWeekly = observer(({cron, disabled}: ISchedule)=>{
    const { t } = useTranslation();
    return (<React.Fragment>
        <div>{t("on")}</div>
        <DaysOfWeekPicker cron={cron} disabled={disabled}/>
        <div>{t("at")}</div>
        <HoursPicker cron={cron} disabled={disabled}/>
        <MinutesPicker cron={cron} disabled={disabled}/>
        <div>{cron?.description() || "---"}</div>
    </React.Fragment>)
});

const ScheduleMonthly = observer(({cron, disabled}: ISchedule)=>{
    const { t } = useTranslation();
    return (<React.Fragment>
        <div>{t("each")}</div>
        <DaysPicker cron={cron} disabled={disabled}/>
        <div>{t("of the month at")}</div>
        <HoursPicker cron={cron} disabled={disabled}/>
        <MinutesPicker cron={cron} disabled={disabled}/>
        <div>{cron?.description() || "---"}</div>
    </React.Fragment>)
});

const ScheduleYearly = observer(({cron, disabled}: ISchedule)=>{
    const { t } = useTranslation();
    return (<React.Fragment>
        <div>{t("on the")}</div>
        <DaysPicker cron={cron} disabled={disabled}/>
        <div>{t("of")}</div>
        <MonthsPicker cron={cron} disabled={disabled}/>
        <div>{t("at")}</div>
        <HoursPicker cron={cron} disabled={disabled}/>
        <MinutesPicker cron={cron} disabled={disabled}/>
        <div>{cron?.description() || "---"}</div>
    </React.Fragment>)
});
