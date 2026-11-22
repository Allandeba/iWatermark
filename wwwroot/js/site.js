const watermarkForm = document.querySelector("#watermark-form");

if (watermarkForm && typeof fetch === "function") {
    const submitButton = watermarkForm.querySelector("button[type=submit]");
    const label = submitButton.querySelector("[data-submit-label]");
    const spinner = submitButton.querySelector(".spinner-border");
    const status = watermarkForm.querySelector("#watermark-status");
    const idleLabel = label.textContent;

    function showStatus(message, state) {
        status.textContent = message;
        status.classList.toggle("visually-hidden", state === "pending");
        status.classList.toggle("mt-2", state !== "pending");
        status.classList.toggle("text-danger", state === "error");
    }

    watermarkForm.addEventListener("submit", async (event) => {
        if (event.defaultPrevented) {
            return;
        }
        event.preventDefault();

        if (submitButton.disabled) {
            return;
        }

        const formData = new FormData(watermarkForm);
        const restoreFocus = document.activeElement === submitButton;
        document.querySelector("#Error")?.remove();
        document.querySelector("#Success")?.remove();
        submitButton.disabled = true;
        spinner.classList.remove("d-none");
        label.textContent = "Processando imagens…";
        showStatus(label.textContent, "pending");

        try {
            const response = await fetch(watermarkForm.action, { method: "POST", body: formData });
            const contentType = response.headers.get("Content-Type")?.split(";")[0].trim().toLowerCase();

            if (!response.ok || contentType !== "application/zip") {
                const message = response.status === 413
                    ? "O lote excede o limite de tamanho. Selecione menos imagens e tente novamente."
                    : "Não foi possível processar as imagens. Confira os arquivos e tente novamente.";
                showStatus(message, "error");
                return;
            }

            const downloadUrl = URL.createObjectURL(await response.blob());
            const download = document.createElement("a");
            download.href = downloadUrl;
            download.download = "images.zip";
            document.body.append(download);
            try {
                download.click();
            } finally {
                download.remove();
                setTimeout(() => URL.revokeObjectURL(downloadUrl), 1000);
            }
            showStatus("Download iniciado.", "success");
        } catch {
            showStatus("Não foi possível concluir o processamento. Tente novamente.", "error");
        } finally {
            submitButton.disabled = false;
            spinner.classList.add("d-none");
            label.textContent = idleLabel;
            if (restoreFocus && document.activeElement === document.body) {
                submitButton.focus();
            }
        }
    });
}
