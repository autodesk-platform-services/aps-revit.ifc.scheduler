import {useTranslation} from "react-i18next";

export const NoProjectSelected = ()=>{
    const { t } = useTranslation();
    return (
        <div style={{display: "flex", alignItems: "center", justifyContent: "center", width: "100%"}}>
            <h1>-- {t("No Project Selected")} --</h1>
        </div>)
}